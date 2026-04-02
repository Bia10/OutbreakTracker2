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

    public DoorReader CreateDoorReader(GameClient gameClient, IEEmemMemory eememMemory) =>
        new(gameClient, eememMemory, _loggerFactory.CreateLogger<DoorReader>(), _doorAddressProviders);

    public EnemiesReader CreateEnemiesReader(GameClient gameClient, IEEmemMemory eememMemory) =>
        new(gameClient, eememMemory, _loggerFactory.CreateLogger<EnemiesReader>());

    public InGamePlayerReader CreateInGamePlayerReader(GameClient gameClient, IEEmemMemory eememMemory) =>
        new(gameClient, eememMemory, _loggerFactory.CreateLogger<InGamePlayerReader>());

    public InGameScenarioReader CreateInGameScenarioReader(GameClient gameClient, IEEmemMemory eememMemory) =>
        new(gameClient, eememMemory, _loggerFactory.CreateLogger<InGameScenarioReader>());

    public LobbyRoomPlayerReader CreateLobbyRoomPlayerReader(GameClient gameClient, IEEmemMemory eememMemory) =>
        new(gameClient, eememMemory, _loggerFactory.CreateLogger<LobbyRoomPlayerReader>());

    public LobbyRoomReader CreateLobbyRoomReader(GameClient gameClient, IEEmemMemory eememMemory) =>
        new(gameClient, eememMemory, _loggerFactory.CreateLogger<LobbyRoomReader>());

    public LobbySlotReader CreateLobbySlotReader(GameClient gameClient, IEEmemMemory eememMemory) =>
        new(gameClient, eememMemory, _loggerFactory.CreateLogger<LobbySlotReader>());
}
