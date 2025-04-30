using Microsoft.Extensions.Logging;
using OutbreakTracker2.Memory;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Readers;
using OutbreakTracker2.PCSX2;
using R3;
using System;
using System.Threading.Tasks;

namespace OutbreakTracker2.App.Services.Data;

// TODO: maybe cache, or observable collections
public sealed class DataManager : IDataManager, IDisposable
{
    private readonly ILogger _logger;

    private DoorReader? _doorReader;
    private EnemiesReader? _enemiesReader;
    private InGamePlayerReader? _inGamePlayerReader;
    private InGameScenarioReader? _inGameScenarioReader;
    private LobbyRoomPlayerReader? _lobbyRoomPlayerReader;
    private LobbyRoomReader? _lobbyRoomReader;
    private LobbySlotReader? _lobbySlotReader;

    private bool _isUpdating;
    private bool _isInitialized;
    private IDisposable? _updateSubscription;

    private readonly Subject<DecodedDoor[]> _doorsSubject = new();
    private readonly Subject<DecodedEnemy[]> _enemiesSubject = new();
    private readonly Subject<DecodedInGamePlayer[]> _inGamePlayersSubject = new();
    private readonly Subject<DecodedScenario> _inGameScenarioSubject = new();
    private readonly Subject<DecodedLobbyRoom> _lobbyRoomSubject = new();
    private readonly Subject<DecodedLobbyRoomPlayer[]> _lobbyRoomPlayersSubject = new();
    private readonly Subject<DecodedLobbySlot[]> _lobbySlotsSubject = new();

    public Observable<DecodedDoor[]> DoorsObservable => _doorsSubject;

    public Observable<DecodedEnemy[]> EnemiesObservable => _enemiesSubject;

    public Observable<DecodedInGamePlayer[]> InGamePlayersObservable => _inGamePlayersSubject;

    public Observable<DecodedScenario> InGameScenarioObservable => _inGameScenarioSubject;

    public Observable<DecodedLobbyRoom> LobbyRoomObservable => _lobbyRoomSubject;

    public Observable<DecodedLobbyRoomPlayer[]> LobbyRoomPlayersObservable => _lobbyRoomPlayersSubject;

    public Observable<DecodedLobbySlot[]> LobbySlotsObservable => _lobbySlotsSubject;

    public DataManager(ILogger<IDataManager> logger)
    {
        _logger = logger;
    }

    public DecodedDoor[] Doors => _doorReader?.DecodedDoors ?? [];

    public DecodedEnemy[] Enemies => _enemiesReader?.DecodedEnemies2 ?? [];

    public DecodedInGamePlayer[] InGamePlayers => _inGamePlayerReader?.DecodedInGamePlayers ?? [];

    public DecodedScenario InGameScenario => _inGameScenarioReader?.DecodedScenario ?? new DecodedScenario();

    public DecodedLobbyRoom LobbyRoom => _lobbyRoomReader?.DecodedLobbyRoom ?? new DecodedLobbyRoom();

    public DecodedLobbyRoomPlayer[] LobbyRoomPlayers => _lobbyRoomPlayerReader?.DecodedLobbyRoomPlayers ?? [];

    public DecodedLobbySlot[] LobbySlots => _lobbySlotReader?.DecodedLobbySlots ?? [];

    public void UpdateDoors()
    {
        _doorReader?.UpdateDoors(true);
        _doorsSubject.OnNext(_doorReader?.DecodedDoors ?? []);
    }

    public void UpdateEnemies()
    {
        _enemiesReader?.UpdateEnemies2(true);
        _enemiesSubject.OnNext(_enemiesReader?.DecodedEnemies2 ?? []);
    }

    public void UpdateInGamePlayer()
    {
        _inGamePlayerReader?.UpdateInGamePlayers(true);
        _inGamePlayersSubject.OnNext(_inGamePlayerReader?.DecodedInGamePlayers ?? []);
    }

    public void UpdateInGameScenario()
    {
        _inGameScenarioReader?.UpdateScenario(true);
        _inGameScenarioSubject.OnNext(_inGameScenarioReader?.DecodedScenario ?? new DecodedScenario());
    }

    public void UpdateLobbyRoom()
    {
        _lobbyRoomReader?.UpdateLobbyRoom(true);
        _lobbyRoomSubject.OnNext(_lobbyRoomReader?.DecodedLobbyRoom ?? new DecodedLobbyRoom());
    }

    public void UpdateLobbyRoomPlayers()
    {
        _lobbyRoomPlayerReader?.UpdateRoomPlayers(true);
        _lobbyRoomPlayersSubject.OnNext(_lobbyRoomPlayerReader?.DecodedLobbyRoomPlayers ?? []);
    }

    public void UpdateLobbySlots()
    {
        _lobbySlotReader?.UpdateLobbySlots(true);
        _lobbySlotsSubject.OnNext(_lobbySlotReader?.DecodedLobbySlots ?? []);
    }

    private DateTime _lastUpdateTime = DateTime.MinValue;
    private DateTime _lastFullUpdateTime = DateTime.MaxValue;
    private readonly TimeSpan _minUpdateInterval = TimeSpan.FromMilliseconds(500); // 2 updates/sec max
    private readonly TimeSpan _fullUpdateInterval = TimeSpan.FromMilliseconds(1000); // 1 updates/sec max

    // TODO: this will need to be rewriten later if we manage to figure way to orient in client state
    public ValueTask UpdateAllAsync()
    {
        if (_isUpdating)
            return ValueTask.CompletedTask;

        DateTime now = DateTime.UtcNow;
        if (now - _lastUpdateTime < _minUpdateInterval)
            return default;

        _isUpdating = true;
        _lastUpdateTime = now;
        _logger.LogInformation("Periodic update started.");

        try
        {
            UpdateDoors();
            UpdateEnemies();
            UpdateInGamePlayer();
            UpdateInGameScenario();

            if (now - _lastFullUpdateTime > _fullUpdateInterval)
            {
                _lastFullUpdateTime = now;
                UpdateLobbyRoom();
                UpdateLobbyRoomPlayers();
                UpdateLobbySlots();
            }

            return ValueTask.CompletedTask;
        }
        finally
        {
            _isUpdating = false;
            _logger.LogInformation("Periodic update completed.");
        }
    }

    public void Initialize(GameClient attachedGameClient)
    {
        ArgumentNullException.ThrowIfNull(attachedGameClient);
        if (_isInitialized) return;

        _logger.LogInformation("Initializing data manager.");

        var eememMemory = new EEmemMemory(attachedGameClient, new MemoryReader());

        _doorReader = new DoorReader(attachedGameClient, eememMemory, _logger);
        _enemiesReader = new EnemiesReader(attachedGameClient, eememMemory, _logger);
        _inGamePlayerReader = new InGamePlayerReader(attachedGameClient, eememMemory, _logger);
        _inGameScenarioReader = new InGameScenarioReader(attachedGameClient, eememMemory, _logger);
        _lobbyRoomPlayerReader = new LobbyRoomPlayerReader(attachedGameClient, eememMemory, _logger);
        _lobbyRoomReader = new LobbyRoomReader(attachedGameClient, eememMemory, _logger);
        _lobbySlotReader = new LobbySlotReader(attachedGameClient, eememMemory, _logger);

        _updateSubscription = Observable.Interval(TimeSpan.FromMilliseconds(333))
            .Select(async _ =>
            {
                await UpdateAllAsync();
                return Unit.Default;
            })
            .Subscribe();

        _isInitialized = true;
        _logger.LogInformation("Data manager has been initialized.");
    }

    public void Dispose()
    {
        _updateSubscription?.Dispose();
        _doorsSubject.Dispose();
        _enemiesSubject.Dispose();
        _inGamePlayersSubject.Dispose();
        _inGameScenarioSubject.Dispose();
        _lobbyRoomSubject.Dispose();
        _lobbyRoomPlayersSubject.Dispose();
        _lobbySlotsSubject.Dispose();
    }
}