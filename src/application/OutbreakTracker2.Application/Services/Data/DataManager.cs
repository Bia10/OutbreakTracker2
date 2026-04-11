using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Services.Launcher;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Readers;
using OutbreakTracker2.PCSX2.Client;
using OutbreakTracker2.PCSX2.EEmem;
using R3;

namespace OutbreakTracker2.Application.Services.Data;

public sealed class DataManager : IDataManager, IDisposable
{
    private readonly ILogger<IDataManager> _logger;
    private readonly IEEmemMemory _eememMemory;
    private readonly IGameReaderFactory _readerFactory;
    private IDisposable? _updateSubscription;
    private IDisposable? _loggingSubscriptions;
    private IDisposable? _processSubscription;
    private CancellationTokenSource? _updateCts;

    private IDoorReader? _doorReader;
    private IEnemiesReader? _enemiesReader;
    private IInGamePlayerReader? _inGamePlayerReader;
    private IInGameScenarioReader? _inGameScenarioReader;
    private ILobbyRoomPlayerReader? _lobbyRoomPlayerReader;
    private ILobbyRoomReader? _lobbyRoomReader;
    private ILobbySlotReader? _lobbySlotReader;

    private readonly GameStateStore _store = new();

    private volatile bool _isInitialized;
    private volatile bool _wasInScenario;

    private IDataObservableSource Store => _store;
    public Observable<DecodedDoor[]> DoorsObservable => Store.DoorsObservable;
    public Observable<DecodedEnemy[]> EnemiesObservable => Store.EnemiesObservable;
    public Observable<DecodedInGamePlayer[]> InGamePlayersObservable => Store.InGamePlayersObservable;
    public Observable<DecodedInGameScenario> InGameScenarioObservable => Store.InGameScenarioObservable;
    public Observable<DecodedLobbyRoom> LobbyRoomObservable => Store.LobbyRoomObservable;
    public Observable<DecodedLobbyRoomPlayer[]> LobbyRoomPlayersObservable => Store.LobbyRoomPlayersObservable;
    public Observable<DecodedLobbySlot[]> LobbySlotsObservable => Store.LobbySlotsObservable;
    public Observable<bool> IsAtLobbyObservable => Store.IsAtLobbyObservable;

    public DecodedDoor[] Doors => _store.DoorsState.Value;
    public DecodedEnemy[] Enemies => _store.EnemiesState.Value;
    public DecodedInGamePlayer[] InGamePlayers => _store.InGamePlayersState.Value;
    public DecodedInGameScenario InGameScenario => _store.InGameScenarioState.Value;
    public DecodedLobbyRoom LobbyRoom => _store.LobbyRoomState.Value;
    public DecodedLobbyRoomPlayer[] LobbyRoomPlayers => _store.LobbyRoomPlayersState.Value;
    public DecodedLobbySlot[] LobbySlots => _store.LobbySlotsState.Value;
    public bool IsAtLobby => _store.IsAtLobbyState.Value;

    private readonly TimeSpan _fastUpdateInterval = TimeSpan.FromMilliseconds(250);
    private readonly TimeSpan _slowUpdateInterval = TimeSpan.FromMilliseconds(500);
    private static readonly IReadOnlySet<string> _activeLobbyStatuses = new HashSet<string>(StringComparer.Ordinal)
    {
        "Waiting",
        "In Game",
        "Full",
        "Creating room",
        "Hosting room",
        "Launching room",
    };

    public DataManager(
        ILogger<DataManager> logger,
        IEEmemMemory eememMemory,
        IProcessLauncher processLauncher,
        IGameReaderFactory readerFactory
    )
    {
        _logger = logger;
        _eememMemory = eememMemory;
        _readerFactory = readerFactory;

        _processSubscription = processLauncher
            .ProcessUpdate.Where(model => !model.IsRunning)
            .Subscribe(_ => StopUpdateLoops());

        SetupObservablesLogging();
    }

    private void SetupObservablesLogging()
    {
        IDisposable[] subscriptions =
        [
            DoorsObservable
                .Do(doors => _logger.LogDebug("Doors data CHANGED: {Count} doors.", doors.Length))
                .Subscribe(),
            EnemiesObservable
                .Do(enemies => _logger.LogDebug("Enemies data CHANGED: {Count} enemies.", enemies.Length))
                .Subscribe(),
            InGamePlayersObservable
                .Do(players => _logger.LogDebug("InGamePlayers data CHANGED: {Count} players.", players.Length))
                .Subscribe(),
            InGameScenarioObservable
                .Select(scenario => scenario.Status)
                .DistinctUntilChanged()
                .Do(status => _logger.LogInformation("InGameScenario STATUS CHANGED: {Status}", status))
                .Subscribe(),
            LobbyRoomObservable
                .Select(lobbyRoom => lobbyRoom.Status)
                .DistinctUntilChanged()
                .Do(status => _logger.LogInformation("LobbyRoom STATUS CHANGED: {Status}", status))
                .Subscribe(),
        ];

        _loggingSubscriptions = Disposable.Combine(subscriptions);
    }

    public async ValueTask InitializeAsync(IGameClient gameClient, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(gameClient);

        if (_isInitialized)
        {
            _logger.LogWarning("DataManager is already initialized. Skipping re-initialization.");
            return;
        }

        _logger.LogInformation("Attempting to initialize DataManager and EEmemory connection");

        bool eememInitialized = await _eememMemory.InitializeAsync(gameClient, cancellationToken).ConfigureAwait(false);
        if (!eememInitialized)
        {
            throw new InvalidOperationException(
                $"Failed to initialize EEmemory for process '{gameClient.Process?.ProcessName}' (PID: {gameClient.Process?.Id})."
            );
        }

        _doorReader = _readerFactory.CreateDoorReader(gameClient, _eememMemory);
        _enemiesReader = _readerFactory.CreateEnemiesReader(gameClient, _eememMemory);
        _inGamePlayerReader = _readerFactory.CreateInGamePlayerReader(gameClient, _eememMemory);
        _inGameScenarioReader = _readerFactory.CreateInGameScenarioReader(gameClient, _eememMemory);
        _lobbyRoomPlayerReader = _readerFactory.CreateLobbyRoomPlayerReader(gameClient, _eememMemory);
        _lobbyRoomReader = _readerFactory.CreateLobbyRoomReader(gameClient, _eememMemory);
        _lobbySlotReader = _readerFactory.CreateLobbySlotReader(gameClient, _eememMemory);

        _updateCts?.Cancel();
        _updateCts?.Dispose();
        _updateCts = new CancellationTokenSource();

        Observable<Unit> fastUpdateTrigger = Observable.Interval(_fastUpdateInterval, _updateCts.Token);
        Observable<Unit> slowUpdateTrigger = Observable.Interval(_slowUpdateInterval, _updateCts.Token);

        IDisposable fastSubscription = fastUpdateTrigger
            .Where(_ => IsInScenario())
            .ObserveOnThreadPool()
            .Subscribe(_ =>
            {
                try
                {
                    UpdateCoreGameData();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during fast update cycle");
                }
            });

        IDisposable slowSubscription = slowUpdateTrigger
            .Where(_ => !IsInScenario())
            .ObserveOnThreadPool()
            .Subscribe(_ =>
            {
                try
                {
                    UpdateLobbyData();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during slow update cycle");
                }
            });

        _updateSubscription = Disposable.Combine(fastSubscription, slowSubscription);

        _isInitialized = true;
        _logger.LogInformation("Data manager has been initialized and update loop started");
    }

    private void UpdateDoors()
    {
        _doorReader?.UpdateDoors();
        _store.DoorsState.Value = _doorReader?.DecodedDoors ?? [];
    }

    private void UpdateEnemies()
    {
        _enemiesReader?.UpdateEnemies2();
        _store.EnemiesState.Value = _enemiesReader?.DecodedEnemies2 ?? [];
    }

    private void UpdateInGamePlayer()
    {
        _inGamePlayerReader?.UpdateInGamePlayers();
        _store.InGamePlayersState.Value = _inGamePlayerReader?.DecodedInGamePlayers ?? [];
    }

    private void UpdateInGameScenario()
    {
        _inGameScenarioReader?.UpdateScenario();
        _store.InGameScenarioState.Value = _inGameScenarioReader?.DecodedScenario ?? new DecodedInGameScenario();
    }

    private void UpdateLobbyRoom()
    {
        _lobbyRoomReader?.UpdateLobbyRoom();
    }

    private void UpdateLobbyRoomPlayers()
    {
        _lobbyRoomPlayerReader?.UpdateRoomPlayers();
        _store.LobbyRoomPlayersState.Value = _lobbyRoomPlayerReader?.DecodedLobbyRoomPlayers ?? [];
    }

    private void UpdateLobbySlots()
    {
        _lobbySlotReader?.UpdateLobbySlots();
        _store.LobbySlotsState.Value = _lobbySlotReader?.DecodedLobbySlots ?? [];
    }

    private void ResetInGameData()
    {
        _logger.LogInformation("Resetting in-game data states to defaults (scenario ended or game stopped)");
        // Reset scenario status FIRST so subscribers (e.g. RunReportService) see Status=None
        // before the enemy/door/player diffs fire. Otherwise the enemy removal diff arrives
        // while _lastScenarioStatus is still InGame, causing ghost kill events for every
        // dead slot (CurHp <= 1) in the fixed 80-slot enemy array.
        _store.InGameScenarioState.Value = new DecodedInGameScenario();
        _store.DoorsState.Value = [];
        _store.EnemiesState.Value = [];
        _store.InGamePlayersState.Value = [];
    }

    private void UpdateCoreGameData()
    {
        _wasInScenario = true;
        _store.IsAtLobbyState.Value = false;
        UpdateInGameScenario();
        UpdateDoors();
        UpdateEnemies();
        UpdateInGamePlayer();
    }

    private void UpdateLobbyData()
    {
        if (_wasInScenario)
        {
            _wasInScenario = false;
            ResetInGameData();
        }

        UpdateLobbyRoom();
        DecodedLobbyRoom lobbyRoom = _lobbyRoomReader?.DecodedLobbyRoom ?? new DecodedLobbyRoom();
        bool isAtLobby = IsLobbyActive(lobbyRoom);

        _store.IsAtLobbyState.Value = isAtLobby;
        if (!isAtLobby)
            return;

        _store.LobbyRoomState.Value = lobbyRoom;
        UpdateLobbyRoomPlayers();
        UpdateLobbySlots();
    }

    private static bool IsLobbyActive(in DecodedLobbyRoom lobbyRoom) =>
        lobbyRoom.CurPlayer is >= 0 and <= GameConstants.MaxPlayers
        && lobbyRoom.MaxPlayer is >= 2 and <= GameConstants.MaxPlayers
        && _activeLobbyStatuses.Contains(lobbyRoom.Status);

    private bool IsInScenario() => _inGameScenarioReader is not null && _inGameScenarioReader.IsInScenario();

    private void StopUpdateLoops()
    {
        _updateCts?.Cancel();

        _updateSubscription?.Dispose();
        _updateSubscription = null;

        _updateCts?.Dispose();
        _updateCts = null;

        _doorReader?.Dispose();
        _doorReader = null;
        _enemiesReader?.Dispose();
        _enemiesReader = null;
        _inGamePlayerReader?.Dispose();
        _inGamePlayerReader = null;
        _inGameScenarioReader?.Dispose();
        _inGameScenarioReader = null;
        _lobbyRoomPlayerReader?.Dispose();
        _lobbyRoomPlayerReader = null;
        _lobbyRoomReader?.Dispose();
        _lobbyRoomReader = null;
        _lobbySlotReader?.Dispose();
        _lobbySlotReader = null;

        _store.IsAtLobbyState.Value = false;
        _wasInScenario = false;
        ResetInGameData();

        _isInitialized = false;
        _logger.LogInformation("DataManager update loops stopped; ready for re-initialization.");
    }

    public void Dispose()
    {
        _logger.LogInformation("Disposing DataManager...");
        _processSubscription?.Dispose();
        StopUpdateLoops();
        _loggingSubscriptions?.Dispose();

        _store.Dispose();

        _logger.LogInformation("DataManager disposed");
    }
}
