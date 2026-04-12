using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Services.Launcher;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Readers;
using OutbreakTracker2.PCSX2.Client;
using OutbreakTracker2.PCSX2.EEmem;
using R3;

namespace OutbreakTracker2.Application.Services.Data;

public sealed class DataManager : IDataManager, ICurrentScenarioState, IDisposable
{
    private int _disposeState;
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
    public Observable<InGameOverviewSnapshot> InGameOverviewObservable => Store.InGameOverviewObservable;
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

    // ICurrentScenarioState — narrow pull contract for alert rules
    string ICurrentScenarioState.ScenarioName => _store.InGameScenarioState.Value.ScenarioName;
    ScenarioStatus ICurrentScenarioState.Status => _store.InGameScenarioState.Value.Status;

    private readonly TimeSpan _fastUpdateInterval = TimeSpan.FromMilliseconds(250);
    private readonly TimeSpan _slowUpdateInterval = TimeSpan.FromMilliseconds(500);

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
        // Add subscriptions to a list incrementally so any exception during Subscribe()
        // allows the catch block to dispose the already-registered subscriptions instead
        // of leaking them (unlike a batch array initializer where partial failures are silent).
        List<IDisposable> subs = new(5);
        try
        {
            subs.Add(
                DoorsObservable
                    .Do(doors => _logger.LogDebug("Doors data CHANGED: {Count} doors.", doors.Length))
                    .Subscribe()
            );
            subs.Add(
                EnemiesObservable
                    .Do(enemies => _logger.LogDebug("Enemies data CHANGED: {Count} enemies.", enemies.Length))
                    .Subscribe()
            );
            subs.Add(
                InGamePlayersObservable
                    .Do(players => _logger.LogDebug("InGamePlayers data CHANGED: {Count} players.", players.Length))
                    .Subscribe()
            );
            subs.Add(
                InGameScenarioObservable
                    .Select(scenario => scenario.Status)
                    .DistinctUntilChanged()
                    .Do(status => _logger.LogInformation("InGameScenario STATUS CHANGED: {Status}", status))
                    .Subscribe()
            );
            subs.Add(
                LobbyRoomObservable
                    .Select(lobbyRoom => lobbyRoom.Status)
                    .DistinctUntilChanged()
                    .Do(status => _logger.LogInformation("LobbyRoom STATUS CHANGED: {Status}", status))
                    .Subscribe()
            );
            _loggingSubscriptions = Disposable.Combine([.. subs]);
        }
        catch
        {
            foreach (IDisposable s in subs)
                s.Dispose();
            throw;
        }
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

        // Each Observable.Interval emits one value at a time on the thread pool. The Subscribe
        // callback runs synchronously per emission, so consecutive Update calls within the same
        // loop are serialized. The fast and slow loops are on separate intervals and can overlap
        // each other, but they write to independent parts of the store (in-game vs lobby data).
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

        // Combine into one handle: if Subscribe() for slowSubscription throws (extremely unlikely),
        // fastSubscription is still disposed when StopUpdateLoops() disposes _updateSubscription.
        // Both are assigned before any exception can escape this path.
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
        PublishInGameOverviewSnapshot();
    }

    private void PublishInGameOverviewSnapshot() =>
        _store.InGameOverviewState.Value = new InGameOverviewSnapshot(
            _store.InGameScenarioState.Value,
            _store.InGamePlayersState.Value,
            _store.EnemiesState.Value,
            _store.DoorsState.Value
        );

    private void UpdateCoreGameData()
    {
        _wasInScenario = true;
        _store.IsAtLobbyState.Value = false;
        UpdateInGameScenario();
        UpdateDoors();
        UpdateEnemies();
        UpdateInGamePlayer();
        PublishInGameOverviewSnapshot();
    }

    // Lobby statuses that unambiguously indicate the game has ended and the room browser is
    // active. "Launching room" and "In Game" are intentionally excluded: they persist for the
    // entire match (including in-game area-load transitions where ScenarioStatus briefly
    // becomes non-InGame) and must never trigger a premature ResetInGameData.
    private static readonly IReadOnlySet<string> _postGameLobbyStatuses = new HashSet<string>(StringComparer.Ordinal)
    {
        "Waiting",
        "Hosting room",
        "Creating room",
        "Full",
    };

    private void UpdateLobbyData()
    {
        UpdateLobbyRoom();
        DecodedLobbyRoom lobbyRoom = _lobbyRoomReader?.DecodedLobbyRoom ?? new DecodedLobbyRoom();

        if (_wasInScenario)
        {
            // Defer the data reset until the lobby confirms we have truly returned to the
            // room browser. During in-game area loads LobbyRoom stays at "Launching room",
            // so the early return here prevents those transient non-InGame intervals from
            // creating spurious session splits in RunReportService.
            if (!_postGameLobbyStatuses.Contains(lobbyRoom.Status))
                return;

            _wasInScenario = false;
            ResetInGameData();
        }

        bool isAtLobby = LobbyStatusPolicy.IsActive(lobbyRoom);
        _store.IsAtLobbyState.Value = isAtLobby;
        if (!isAtLobby)
            return;

        _store.LobbyRoomState.Value = lobbyRoom;
        UpdateLobbyRoomPlayers();
        UpdateLobbySlots();
    }

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

        try
        {
            _store.IsAtLobbyState.Value = false;
            _wasInScenario = false;
            ResetInGameData();
        }
        catch (ObjectDisposedException) when (Volatile.Read(ref _disposeState) != 0)
        {
            // Ignore late stop requests racing with disposal.
        }

        _isInitialized = false;
        _logger.LogInformation("DataManager update loops stopped; ready for re-initialization.");
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0)
            return;

        _logger.LogInformation("Disposing DataManager...");
        _processSubscription?.Dispose();
        StopUpdateLoops();
        _loggingSubscriptions?.Dispose();

        _store.Dispose();

        _logger.LogInformation("DataManager disposed");
    }
}
