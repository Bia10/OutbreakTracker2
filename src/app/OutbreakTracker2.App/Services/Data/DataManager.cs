using Microsoft.Extensions.Logging;
using OutbreakTracker2.Memory.MemoryReader;
using OutbreakTracker2.Memory.StringReader;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Readers;
using OutbreakTracker2.PCSX2;
using R3;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OutbreakTracker2.App.Services.Data;

// TODO: some kind of guarantee of uniqueness before emitting would be nice
// currently we emit the same data over and over
// but that could imply having to wrangle with how to compare the composite data
public sealed class DataManager : IDataManager, IDisposable
{
    private readonly ILogger<IDataManager> _logger;
    private IDisposable? _updateSubscription;
    private EEmemMemory? _eememMemory;
    private DoorReader? _doorReader;
    private EnemiesReader? _enemiesReader;
    private InGamePlayerReader? _inGamePlayerReader;
    private InGameScenarioReader? _inGameScenarioReader;
    private LobbyRoomPlayerReader? _lobbyRoomPlayerReader;
    private LobbyRoomReader? _lobbyRoomReader;
    private LobbySlotReader? _lobbySlotReader;

    private readonly Subject<DecodedDoor[]> _doorsSubject = new();
    private readonly Subject<DecodedEnemy[]> _enemiesSubject = new();
    private readonly Subject<DecodedInGamePlayer[]> _inGamePlayersSubject = new();
    private readonly Subject<DecodedInGameScenario> _inGameScenarioSubject = new();
    private readonly Subject<DecodedLobbyRoom> _lobbyRoomSubject = new();
    private readonly Subject<DecodedLobbyRoomPlayer[]> _lobbyRoomPlayersSubject = new();
    private readonly Subject<DecodedLobbySlot[]> _lobbySlotsSubject = new();

    public bool IsInitialized { get; private set; }

    public Observable<DecodedDoor[]> DoorsObservable { get; private set; } = Observable.Empty<DecodedDoor[]>();
    public Observable<DecodedEnemy[]> EnemiesObservable { get; private set; } = Observable.Empty<DecodedEnemy[]>();
    public Observable<DecodedInGamePlayer[]> InGamePlayersObservable { get; private set; } = Observable.Empty<DecodedInGamePlayer[]>();
    public Observable<DecodedInGameScenario> InGameScenarioObservable { get; private set; } = Observable.Empty<DecodedInGameScenario>();
    public Observable<DecodedLobbyRoom> LobbyRoomObservable { get; private set; } = Observable.Empty<DecodedLobbyRoom>();
    public Observable<DecodedLobbyRoomPlayer[]> LobbyRoomPlayersObservable { get; private set; } = Observable.Empty<DecodedLobbyRoomPlayer[]>();
    public Observable<DecodedLobbySlot[]> LobbySlotsObservable { get; private set; } = Observable.Empty<DecodedLobbySlot[]>();

    public DecodedDoor[] Doors => _doorReader?.DecodedDoors ?? [];
    public DecodedEnemy[] Enemies => _enemiesReader?.DecodedEnemies2 ?? [];
    public DecodedInGamePlayer[] InGamePlayers => _inGamePlayerReader?.DecodedInGamePlayers ?? [];
    public DecodedInGameScenario InGameScenario => _inGameScenarioReader?.DecodedScenario ?? new DecodedInGameScenario();
    public DecodedLobbyRoom LobbyRoom => _lobbyRoomReader?.DecodedLobbyRoom ?? new DecodedLobbyRoom();
    public DecodedLobbyRoomPlayer[] LobbyRoomPlayers => _lobbyRoomPlayerReader?.DecodedLobbyRoomPlayers ?? [];
    public DecodedLobbySlot[] LobbySlots => _lobbySlotReader?.DecodedLobbySlots ?? [];

    private readonly TimeSpan _fastUpdateInterval = TimeSpan.FromMilliseconds(500);
    private readonly TimeSpan _slowUpdateInterval = TimeSpan.FromMilliseconds(1000);

    public DataManager(ILogger<DataManager> logger)
    {
        _logger = logger;
        SetupObservables();
    }

    private void SetupObservables()
    {
        DoorsObservable = _doorsSubject.AsObservable();
        EnemiesObservable = _enemiesSubject.AsObservable();
        InGamePlayersObservable = _inGamePlayersSubject.AsObservable();
        InGameScenarioObservable = _inGameScenarioSubject.AsObservable();
        LobbyRoomObservable = _lobbyRoomSubject.AsObservable();
        LobbyRoomPlayersObservable = _lobbyRoomPlayersSubject.AsObservable();
        LobbySlotsObservable = _lobbySlotsSubject.AsObservable();
    }

    public async ValueTask InitializeAsync(GameClient gameClient, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(gameClient);
        if (IsInitialized) return;

        _logger.LogInformation("Initializing data manager...");

        if (IsInitialized)
        {
            _logger.LogWarning("DataManager is already initialized");
            return;
        }

        _logger.LogInformation("Attempting to initialize DataManager and EEmemory connection");

        _eememMemory = new EEmemMemory(gameClient, new SafeMemoryReader(), new StringReader());

        bool eememMemoryReady = await _eememMemory.InitializeAsync(cancellationToken).ConfigureAwait(false);
        if (!eememMemoryReady)
        {
            _logger.LogError("Failed to initialize EEmemory after multiple attempts within DataManager");
            throw new InvalidOperationException("Failed to initialize EEmemory: PCSX2 memory not ready or 'EEmem' export not found.");
        }

        _doorReader = new DoorReader(gameClient, _eememMemory, _logger);
        _enemiesReader = new EnemiesReader(gameClient, _eememMemory, _logger);
        _inGamePlayerReader = new InGamePlayerReader(gameClient, _eememMemory, _logger);
        _inGameScenarioReader = new InGameScenarioReader(gameClient, _eememMemory, _logger);
        _lobbyRoomPlayerReader = new LobbyRoomPlayerReader(gameClient, _eememMemory, _logger);
        _lobbyRoomReader = new LobbyRoomReader(gameClient, _eememMemory, _logger);
        _lobbySlotReader = new LobbySlotReader(gameClient, _eememMemory, _logger);

        Observable<Unit> fastUpdateTrigger = Observable.Interval(_fastUpdateInterval, cancellationToken);
        Observable<Unit> slowUpdateTrigger = Observable.Interval(_slowUpdateInterval, cancellationToken);

        IDisposable fastSubscription = fastUpdateTrigger
            .Where(_ => IsInScenario())
            .ObserveOnThreadPool()
            .SubscribeAwait(async ValueTask (_, ct) =>
            {
                try
                {
                    await UpdateCoreGameDataAsync(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogTrace("Fast update cancelled");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during fast update cycle");
                }
            }, AwaitOperation.Drop);

        IDisposable slowSubscription = slowUpdateTrigger
            .Where(_ => !IsInScenario())
            .ObserveOnThreadPool()
            .SubscribeAwait(async ValueTask (_, ct) =>
            {
                try
                {
                    await UpdateLobbyDataAsync(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogTrace("Slow update cancelled");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during slow update cycle");
                }
            }, AwaitOperation.Drop);

        _updateSubscription = Disposable.Combine(fastSubscription, slowSubscription);

        IsInitialized = true;
        _logger.LogInformation("Data manager has been initialized and update loop started");
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
        catch (OperationCanceledException ex)
        {
            return ValueTask.FromCanceled(ex.CancellationToken);
        }
        catch (Exception ex)
        {
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
        catch (OperationCanceledException ex)
        {
            return ValueTask.FromCanceled(ex.CancellationToken);
        }
        catch (Exception ex)
        {
            return ValueTask.FromException(ex);
        }
    }

    public void UpdateDoors()
    {
        _doorReader?.UpdateDoors();
        _doorsSubject.OnNext(_doorReader?.DecodedDoors ?? []);
    }

    public void UpdateEnemies()
    {
        _enemiesReader?.UpdateEnemies2();
        _enemiesSubject.OnNext(_enemiesReader?.DecodedEnemies2 ?? []);
    }

    public void UpdateInGamePlayer()
    {
        _inGamePlayerReader?.UpdateInGamePlayers();

        DecodedInGamePlayer[]? decodedPlayers = _inGamePlayerReader?.DecodedInGamePlayers;

        if (decodedPlayers is null)
        {
            _logger.LogWarning("DataManager is about to emit 0 players because the decoded players array is null");
            _inGamePlayersSubject.OnNext([]);
            return;
        }

        _logger.LogDebug("DataManager is about to emit {Count} players", decodedPlayers.Length);

        foreach (DecodedInGamePlayer player in decodedPlayers)
            _logger.LogTrace(
                "DataManager Emitting Player - Name: '{Name}', Pos({X:F2}, {Y:F2}), Health:{Health}/{MaxHealth}({HealthPct:F2}%), Status:'{Status}', Condition:'{Condition}'",
                player.CharacterName, player.PositionX, player.PositionY, player.CurrentHealth, player.MaximumHealth, player.HealthPercentage, player.Status, player.Condition);

        _inGamePlayersSubject.OnNext(decodedPlayers);
    }

    public void UpdateInGameScenario()
    {
        _inGameScenarioReader?.UpdateScenario();
        _inGameScenarioSubject.OnNext(_inGameScenarioReader?.DecodedScenario ?? new DecodedInGameScenario());
    }

    public void UpdateLobbyRoom()
    {
        _lobbyRoomReader?.UpdateLobbyRoom();
        _lobbyRoomSubject.OnNext(_lobbyRoomReader?.DecodedLobbyRoom ?? new DecodedLobbyRoom());
    }

    public void UpdateLobbyRoomPlayers()
    {
        _lobbyRoomPlayerReader?.UpdateRoomPlayers();
        _lobbyRoomPlayersSubject.OnNext(_lobbyRoomPlayerReader?.DecodedLobbyRoomPlayers ?? []);
    }

    public void UpdateLobbySlots()
    {
        _lobbySlotReader?.UpdateLobbySlots();
        _lobbySlotsSubject.OnNext(_lobbySlotReader?.DecodedLobbySlots ?? []);
    }

    private bool IsInScenario()
        => _inGameScenarioReader is not null && _inGameScenarioReader.IsInScenario();

    public void Dispose()
    {
        _logger.LogInformation("Disposing DataManager...");
        _updateSubscription?.Dispose();

        _doorsSubject.OnCompleted();
        _doorsSubject.Dispose();

        _enemiesSubject.OnCompleted();
        _enemiesSubject.Dispose();

        _inGamePlayersSubject.OnCompleted();
        _inGamePlayersSubject.Dispose();

        _inGameScenarioSubject.OnCompleted();
        _inGameScenarioSubject.Dispose();

        _lobbyRoomSubject.OnCompleted();
        _lobbyRoomSubject.Dispose();

        _lobbyRoomPlayersSubject.OnCompleted();
        _lobbyRoomPlayersSubject.Dispose();

        _lobbySlotsSubject.OnCompleted();
        _lobbySlotsSubject.Dispose();

        IsInitialized = false;
        _logger.LogInformation("DataManager disposed");
    }
}