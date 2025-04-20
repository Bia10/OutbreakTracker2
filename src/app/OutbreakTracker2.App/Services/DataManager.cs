using OutbreakTracker2.Memory;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Readers;
using OutbreakTracker2.PCSX2;

namespace OutbreakTracker2.App.Services;

// TODO: maybe cache, or observable collections
public class DataManager
{
    private readonly DoorReader _doorReader;
    private readonly EnemiesReader _enemiesReader;
    private readonly InGamePlayerReader _inGamePlayerReader;
    private readonly InGameScenarioReader _inGameScenarioReader;
    private readonly LobbyRoomPlayerReader _lobbyRoomPlayerReader;
    private readonly LobbyRoomReader _lobbyRoomReader;
    private readonly LobbySlotReader _lobbySlotReader;

    private bool isInitialized;

    public DataManager()
    {
        // TODO: doesnt belong here
        using var gameClient = new GameClient();
        gameClient.AttachToPCSX2();

        var memoryReader = new MemoryReader();
        var eememMemory = new EEmemMemory(gameClient, memoryReader);

        _doorReader = new DoorReader(gameClient, eememMemory);
        _enemiesReader = new EnemiesReader(gameClient, eememMemory);
        _inGamePlayerReader = new InGamePlayerReader(gameClient, eememMemory);
        _inGameScenarioReader = new InGameScenarioReader(gameClient, eememMemory);
        _lobbyRoomPlayerReader = new LobbyRoomPlayerReader(gameClient, eememMemory);
        _lobbyRoomReader = new LobbyRoomReader(gameClient, eememMemory);
        _lobbySlotReader = new LobbySlotReader(gameClient, eememMemory);
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

    public void UpdateAll()
    {
        UpdateDoors();
        UpdateEnemies();
        UpdateInGamePlayer();
        UpdateInGameScenario();
        UpdateLobbyRoom();
        UpdateLobbyRoomPlayers();
        UpdateLobbySlots();
    }

    public void Initialize()
    {
        if (isInitialized)
            return;

        UpdateAll();

        isInitialized = true;
    }
}