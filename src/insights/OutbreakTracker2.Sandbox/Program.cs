﻿using OutbreakTracker2.Memory;
using OutbreakTracker2.Outbreak.Readers;
using OutbreakTracker2.PCSX2Memory;

namespace OutbreakTracker2.Sandbox;

public class Program
{
    static void Main(string[] _)
    {
        using var gameClient = new GameClient();
        gameClient.AttachToPCSX2();

        var memoryReader = new MemoryReader();
        var eememMemory = new EEmemMemory(gameClient, memoryReader);
        var lobbyReader = new LobbySlotReader(gameClient, eememMemory);
        var lobbyRoomReader = new LobbyRoomReader(gameClient, eememMemory);

        lobbyReader.UpdateLobbySlots(debug: true);
        lobbyRoomReader.UpdateLobbyRoom(debug: true);
    }
}
