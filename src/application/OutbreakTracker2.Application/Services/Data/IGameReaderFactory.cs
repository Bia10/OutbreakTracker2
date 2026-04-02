using OutbreakTracker2.Outbreak.Readers;
using OutbreakTracker2.PCSX2.Client;
using OutbreakTracker2.PCSX2.EEmem;

namespace OutbreakTracker2.Application.Services.Data;

public interface IGameReaderFactory
{
    DoorReader CreateDoorReader(GameClient gameClient, IEEmemMemory eememMemory);

    EnemiesReader CreateEnemiesReader(GameClient gameClient, IEEmemMemory eememMemory);

    InGamePlayerReader CreateInGamePlayerReader(GameClient gameClient, IEEmemMemory eememMemory);

    InGameScenarioReader CreateInGameScenarioReader(GameClient gameClient, IEEmemMemory eememMemory);

    LobbyRoomPlayerReader CreateLobbyRoomPlayerReader(GameClient gameClient, IEEmemMemory eememMemory);

    LobbyRoomReader CreateLobbyRoomReader(GameClient gameClient, IEEmemMemory eememMemory);

    LobbySlotReader CreateLobbySlotReader(GameClient gameClient, IEEmemMemory eememMemory);
}
