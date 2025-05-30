using Microsoft.Extensions.Logging;
using OutbreakTracker2.App.Comparers;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Readers;
using OutbreakTracker2.PCSX2.Client;
using OutbreakTracker2.PCSX2.EEmem;
using R3;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OutbreakTracker2.App.Services.Data;

public sealed class DataManager : IDataManager, IDisposable
{
    private readonly ILogger<IDataManager> _logger;
    private readonly IEEmemMemory _eememMemory;
    private GameClient? _gameClient;
    private IDisposable? _updateSubscription;
    private IDisposable? _loggingSubscriptions;

    private DoorReader? _doorReader;
    private EnemiesReader? _enemiesReader;
    private InGamePlayerReader? _inGamePlayerReader;
    private InGameScenarioReader? _inGameScenarioReader;
    private LobbyRoomPlayerReader? _lobbyRoomPlayerReader;
    private LobbyRoomReader? _lobbyRoomReader;
    private LobbySlotReader? _lobbySlotReader;

    private readonly ReactiveProperty<DecodedDoor[]> _doorsState = new([]);
    private readonly ReactiveProperty<DecodedEnemy[]> _enemiesState = new([]);
    private readonly ReactiveProperty<DecodedInGamePlayer[]> _inGamePlayersState = new([]);
    private readonly ReactiveProperty<DecodedInGameScenario> _inGameScenarioState = new(new DecodedInGameScenario());
    private readonly ReactiveProperty<DecodedLobbyRoom> _lobbyRoomState = new(new DecodedLobbyRoom());
    private readonly ReactiveProperty<DecodedLobbyRoomPlayer[]> _lobbyRoomPlayersState = new([]);
    private readonly ReactiveProperty<DecodedLobbySlot[]> _lobbySlotsState = new([]);

    private bool IsInitialized { get; set; }

    public Observable<DecodedDoor[]> DoorsObservable { get; }
    public Observable<DecodedEnemy[]> EnemiesObservable { get; }
    public Observable<DecodedInGamePlayer[]> InGamePlayersObservable { get; }
    public Observable<DecodedInGameScenario> InGameScenarioObservable { get; }
    public Observable<DecodedLobbyRoom> LobbyRoomObservable { get; }
    public Observable<DecodedLobbyRoomPlayer[]> LobbyRoomPlayersObservable { get; }
    public Observable<DecodedLobbySlot[]> LobbySlotsObservable { get; }

    public DecodedDoor[] Doors => _doorsState.Value;
    public DecodedEnemy[] Enemies => _enemiesState.Value;
    public DecodedInGamePlayer[] InGamePlayers => _inGamePlayersState.Value;
    public DecodedInGameScenario InGameScenario => _inGameScenarioState.Value;
    public DecodedLobbyRoom LobbyRoom => _lobbyRoomState.Value;
    public DecodedLobbyRoomPlayer[] LobbyRoomPlayers => _lobbyRoomPlayersState.Value;
    public DecodedLobbySlot[] LobbySlots => _lobbySlotsState.Value;

    private readonly TimeSpan _fastUpdateInterval = TimeSpan.FromMilliseconds(250);
    private readonly TimeSpan _slowUpdateInterval = TimeSpan.FromMilliseconds(500);

    public DataManager(ILogger<DataManager> logger, IEEmemMemory eememMemory)
    {
        _logger = logger;
        _eememMemory = eememMemory;

        DoorsObservable = _doorsState.DistinctUntilChanged(new ArraySequenceComparer<DecodedDoor>());
        EnemiesObservable = _enemiesState.DistinctUntilChanged(new ArraySequenceComparer<DecodedEnemy>());
        InGamePlayersObservable = _inGamePlayersState.DistinctUntilChanged(new ArraySequenceComparer<DecodedInGamePlayer>());
        InGameScenarioObservable = _inGameScenarioState.DistinctUntilChanged();
        LobbyRoomObservable = _lobbyRoomState.DistinctUntilChanged();
        LobbyRoomPlayersObservable = _lobbyRoomPlayersState.DistinctUntilChanged(new ArraySequenceComparer<DecodedLobbyRoomPlayer>());
        LobbySlotsObservable = _lobbySlotsState.DistinctUntilChanged(new ArraySequenceComparer<DecodedLobbySlot>());

        SetupObservablesLogging();
    }

    private void SetupObservablesLogging()
    {
        IDisposable[] subscriptions =
        [
            DoorsObservable.Do(doors
                    => _logger.LogInformation("Doors data CHANGED: {Count} doors.", doors.Length))
                .Subscribe(),

            EnemiesObservable.Do(enemies
                    => _logger.LogInformation("Enemies data CHANGED: {Count} enemies.", enemies.Length))
                .Subscribe(),

            InGamePlayersObservable.Do(players
                    => _logger.LogInformation("InGamePlayers data CHANGED: {Count} players.", players.Length))
                .Subscribe(),

            InGameScenarioObservable
                .Select(scenario => scenario.Status)
                .DistinctUntilChanged()
                .Do(status => _logger.LogWarning("InGameScenario STATUS CHANGED: {Status}", status))
                .Subscribe(),

            LobbyRoomObservable.Select(lobbyRoom => lobbyRoom.Status)
                .DistinctUntilChanged()
                .Do(status => _logger.LogWarning("LobbyRoom STATUS CHANGED: {Status}", status))
                .Subscribe(),
        ];

        _loggingSubscriptions = Disposable.Combine(subscriptions);
    }

    public async ValueTask InitializeAsync(GameClient gameClient, CancellationToken cancellationToken)
    {
        _gameClient = gameClient ?? throw new ArgumentNullException(nameof(gameClient));

        if (IsInitialized)
        {
            _logger.LogWarning("DataManager is already initialized. Skipping re-initialization.");
            return;
        }

        _logger.LogInformation("Attempting to initialize DataManager and EEmemory connection");

        bool eememInitialized = await _eememMemory.InitializeAsync(_gameClient, cancellationToken);
        if (!eememInitialized)
        {
            throw new InvalidOperationException("Failed to initialize EEmemory.");
        }

        _doorReader = new DoorReader(_gameClient, _eememMemory, _logger);
        _enemiesReader = new EnemiesReader(_gameClient, _eememMemory, _logger);
        _inGamePlayerReader = new InGamePlayerReader(_gameClient, _eememMemory, _logger);
        _inGameScenarioReader = new InGameScenarioReader(_gameClient, _eememMemory, _logger);
        _lobbyRoomPlayerReader = new LobbyRoomPlayerReader(_gameClient, _eememMemory, _logger);
        _lobbyRoomReader = new LobbyRoomReader(_gameClient, _eememMemory, _logger);
        _lobbySlotReader = new LobbySlotReader(_gameClient, _eememMemory, _logger);

        Observable<Unit> fastUpdateTrigger = Observable.Interval(_fastUpdateInterval, cancellationToken);
        Observable<Unit> slowUpdateTrigger = Observable.Interval(_slowUpdateInterval, cancellationToken);

        IDisposable fastSubscription = fastUpdateTrigger
            .Where(_ => IsInScenario())
            .ObserveOnThreadPool()
            .SubscribeAwait(async ValueTask (_, ct) =>
            {
                try { await UpdateCoreGameDataAsync(ct).ConfigureAwait(false); }
                catch (OperationCanceledException) { _logger.LogTrace("Fast update cancelled"); }
                catch (Exception ex) { _logger.LogError(ex, "Error during fast update cycle"); }
            }, AwaitOperation.Drop);

        IDisposable slowSubscription = slowUpdateTrigger
            .Where(_ => !IsInScenario())
            .ObserveOnThreadPool()
            .SubscribeAwait(async ValueTask (_, ct) =>
            {
                try { await UpdateLobbyDataAsync(ct).ConfigureAwait(false); }
                catch (OperationCanceledException) { _logger.LogTrace("Slow update cancelled"); }
                catch (Exception ex) { _logger.LogError(ex, "Error during slow update cycle"); }
            }, AwaitOperation.Drop);

        _updateSubscription = Disposable.Combine(fastSubscription, slowSubscription);

        IsInitialized = true;
        _logger.LogInformation("Data manager has been initialized and update loop started");
    }

    public void UpdateDoors()
    {
        _doorReader?.UpdateDoors();
        _doorsState.Value = _doorReader?.DecodedDoors ?? [];
    }

    public void UpdateEnemies()
    {
        _enemiesReader?.UpdateEnemies2();
        _enemiesState.Value = _enemiesReader?.DecodedEnemies2 ?? [];
    }

    public void UpdateInGamePlayer()
    {
        _inGamePlayerReader?.UpdateInGamePlayers();
        _inGamePlayersState.Value = _inGamePlayerReader?.DecodedInGamePlayers ?? [];
    }

    public void UpdateInGameScenario()
    {
        _inGameScenarioReader?.UpdateScenario();
        _inGameScenarioState.Value = _inGameScenarioReader?.DecodedScenario ?? new DecodedInGameScenario();
    }

    public void UpdateLobbyRoom()
    {
        _lobbyRoomReader?.UpdateLobbyRoom();
        _lobbyRoomState.Value = _lobbyRoomReader?.DecodedLobbyRoom ?? new DecodedLobbyRoom();
    }

    public void UpdateLobbyRoomPlayers()
    {
        _lobbyRoomPlayerReader?.UpdateRoomPlayers();
        _lobbyRoomPlayersState.Value = _lobbyRoomPlayerReader?.DecodedLobbyRoomPlayers ?? [];
    }

    public void UpdateLobbySlots()
    {
        _lobbySlotReader?.UpdateLobbySlots();
        _lobbySlotsState.Value = _lobbySlotReader?.DecodedLobbySlots ?? [];
    }

    private ValueTask UpdateCoreGameDataAsync(CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            UpdateInGameScenario();
            UpdateDoors();
            UpdateEnemies();
            UpdateInGamePlayer();
            return ValueTask.CompletedTask;
        }
        catch (OperationCanceledException ex) { return ValueTask.FromCanceled(ex.CancellationToken); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during core game data update.");
            return ValueTask.FromException(ex);
        }
    }

    private ValueTask UpdateLobbyDataAsync(CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            UpdateLobbyRoom();
            UpdateLobbyRoomPlayers();
            UpdateLobbySlots();
            return ValueTask.CompletedTask;
        }
        catch (OperationCanceledException ex) { return ValueTask.FromCanceled(ex.CancellationToken); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during lobby data update.");
            return ValueTask.FromException(ex);
        }
    }

    private bool IsInScenario() => _inGameScenarioReader is not null && _inGameScenarioReader.IsInScenario();

    public void Dispose()
    {
        _logger.LogInformation("Disposing DataManager...");
        _updateSubscription?.Dispose();
        _loggingSubscriptions?.Dispose();

        _doorsState.Dispose();
        _enemiesState.Dispose();
        _inGamePlayersState.Dispose();
        _inGameScenarioState.Dispose();
        _lobbyRoomState.Dispose();
        _lobbyRoomPlayersState.Dispose();
        _lobbySlotsState.Dispose();

        IsInitialized = false;
        _logger.LogInformation("DataManager disposed");
    }
}