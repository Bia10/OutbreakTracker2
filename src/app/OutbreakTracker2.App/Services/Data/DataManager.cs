using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Memory;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Readers;
using OutbreakTracker2.PCSX2;

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
    private Timer? _updateTimer;
    private readonly Lock _updateLock = new();
    private bool _isUpdating;
    private bool _isInitialized;

    public DataManager(ILogger<IDataManager> logger)
    {
        _logger = logger;
    }

    public DecodedDoor[] Doors => _doorReader.DecodedDoors;
    public DecodedEnemy[] Enemies => _enemiesReader.DecodedEnemies2;
    public DecodedInGamePlayer[] InGamePlayers => _inGamePlayerReader.DecodedInGamePlayers;
    public DecodedScenario InGameScenario => _inGameScenarioReader.DecodedScenario;
    public DecodedLobbyRoom LobbyRoom => _lobbyRoomReader.DecodedLobbyRoom;
    public DecodedLobbyRoomPlayer[] LobbyRoomPlayers => _lobbyRoomPlayerReader.DecodedLobbyRoomPlayers;
    public DecodedLobbySlot[] LobbySlots => _lobbySlotReader.DecodedLobbySlots;

    public void UpdateDoors() => _doorReader.UpdateDoors(true);

    public void UpdateEnemies() => _enemiesReader.UpdateEnemies2(true);

    public void UpdateInGamePlayer() => _inGamePlayerReader.UpdateInGamePlayers(true);

    public void UpdateInGameScenario() => _inGameScenarioReader.UpdateScenario(true);

    public void UpdateLobbyRoom() => _lobbyRoomReader.UpdateLobbyRoom(true);

    public void UpdateLobbyRoomPlayers() => _lobbyRoomPlayerReader.UpdateRoomPlayers(true);

    public void UpdateLobbySlots() => _lobbySlotReader.UpdateLobbySlots(true);

    public void UpdateAll(object? state)
    {
        if (_isUpdating) return;

        lock (_updateLock)
        {
            _isUpdating = true;
            _logger.LogInformation("Periodic update started.");
            try
            {
                UpdateDoors();
                UpdateEnemies();
                UpdateInGamePlayer();
                UpdateInGameScenario();
                UpdateLobbyRoom();
                UpdateLobbyRoomPlayers();
                UpdateLobbySlots();
            }
            finally
            {
                _isUpdating = false;
                _logger.LogInformation("Periodic update completed.");
            }
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
        _updateTimer = new Timer(UpdateAll, null, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(1));

        _isInitialized = true;
        _logger.LogInformation("Data manager has been initialized.");
    }

    public void Dispose()
    {
        _updateTimer?.Dispose();
    }
}