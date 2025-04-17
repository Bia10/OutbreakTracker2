using OutbreakTracker2.Memory;
using OutbreakTracker2.Outbreak.Readers;
using OutbreakTracker2.PCSX2;

namespace OutbreakTracker2.Sandbox;

public class Program
{
    private static void Main(string[] _)
    {
        using var gameClient = new GameClient();
        gameClient.AttachToPCSX2();

        var memoryReader = new MemoryReader();
        var eememMemory = new EEmemMemory(gameClient, memoryReader);
        var lobbyReader = new LobbySlotReader(gameClient, eememMemory);
        var lobbyRoomReader = new LobbyRoomReader(gameClient, eememMemory);
        var lobbyRoomPlayerReader = new LobbyRoomPlayerReader(gameClient, eememMemory);
        var doorReader = new DoorReader(gameClient, eememMemory);

        lobbyReader.UpdateLobbySlots(debug: true);
        lobbyRoomReader.UpdateLobbyRoom(debug: true);
        lobbyRoomPlayerReader.UpdateRoomPlayers(debug: true);
        doorReader.UpdateDoors(debug: true);
    }
}
