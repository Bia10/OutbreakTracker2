namespace OutbreakTracker2.Sandbox;

public class Program
{
    static void Main(string[] _)
    {
        using var gameClient = new GameClient();
        gameClient.AttachToPCSX2();

        var memoryReader = new MemoryReader();
        var eememMemory = new EEmemMemory(gameClient, memoryReader);
        var lobbyReader = new LobbyReader(gameClient, eememMemory);
        lobbyReader.UpdateLobbies(debug: true);
    }
}
