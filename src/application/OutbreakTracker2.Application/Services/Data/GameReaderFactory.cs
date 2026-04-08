using Microsoft.Extensions.Logging;
using OutbreakTracker2.Outbreak.Readers;
using OutbreakTracker2.PCSX2.Client;
using OutbreakTracker2.PCSX2.EEmem;

namespace OutbreakTracker2.Application.Services.Data;

public sealed class GameReaderFactory(
    ILoggerFactory loggerFactory,
    IEnumerable<IDoorAddressProvider> doorAddressProviders
) : IGameReaderFactory
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    private readonly IEnumerable<IDoorAddressProvider> _doorAddressProviders = doorAddressProviders;

    public IDoorReader CreateDoorReader(IGameClient gameClient, IEEmemAddressReader eememMemory) =>
        new DoorReader(gameClient, eememMemory, _loggerFactory.CreateLogger<DoorReader>(), _doorAddressProviders);

    public IEnemiesReader CreateEnemiesReader(IGameClient gameClient, IEEmemAddressReader eememMemory) =>
        new EnemiesReader(gameClient, eememMemory, _loggerFactory.CreateLogger<EnemiesReader>());

    public IInGamePlayerReader CreateInGamePlayerReader(IGameClient gameClient, IEEmemAddressReader eememMemory) =>
        new InGamePlayerReader(gameClient, eememMemory, _loggerFactory.CreateLogger<InGamePlayerReader>());

    public IInGameScenarioReader CreateInGameScenarioReader(IGameClient gameClient, IEEmemAddressReader eememMemory) =>
        new InGameScenarioReader(gameClient, eememMemory, _loggerFactory.CreateLogger<InGameScenarioReader>());

    public ILobbyRoomPlayerReader CreateLobbyRoomPlayerReader(
        IGameClient gameClient,
        IEEmemAddressReader eememMemory
    ) => new LobbyRoomPlayerReader(gameClient, eememMemory, _loggerFactory.CreateLogger<LobbyRoomPlayerReader>());

    public ILobbyRoomReader CreateLobbyRoomReader(IGameClient gameClient, IEEmemAddressReader eememMemory) =>
        new LobbyRoomReader(gameClient, eememMemory, _loggerFactory.CreateLogger<LobbyRoomReader>());

    public ILobbySlotReader CreateLobbySlotReader(IGameClient gameClient, IEEmemAddressReader eememMemory) =>
        new LobbySlotReader(gameClient, eememMemory, _loggerFactory.CreateLogger<LobbySlotReader>());
}
