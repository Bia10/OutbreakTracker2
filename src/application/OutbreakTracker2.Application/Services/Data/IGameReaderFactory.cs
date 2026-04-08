using OutbreakTracker2.Outbreak.Readers;
using OutbreakTracker2.PCSX2.Client;
using OutbreakTracker2.PCSX2.EEmem;

namespace OutbreakTracker2.Application.Services.Data;

public interface IGameReaderFactory
{
    IDoorReader CreateDoorReader(IGameClient gameClient, IEEmemAddressReader eememMemory);

    IEnemiesReader CreateEnemiesReader(IGameClient gameClient, IEEmemAddressReader eememMemory);

    IInGamePlayerReader CreateInGamePlayerReader(IGameClient gameClient, IEEmemAddressReader eememMemory);

    IInGameScenarioReader CreateInGameScenarioReader(IGameClient gameClient, IEEmemAddressReader eememMemory);

    ILobbyRoomPlayerReader CreateLobbyRoomPlayerReader(IGameClient gameClient, IEEmemAddressReader eememMemory);

    ILobbyRoomReader CreateLobbyRoomReader(IGameClient gameClient, IEEmemAddressReader eememMemory);

    ILobbySlotReader CreateLobbySlotReader(IGameClient gameClient, IEEmemAddressReader eememMemory);
}
