using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Views.Common.ScenarioImg;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.LobbySlot.Factory;

public sealed class LobbySlotViewModelFactory(
    ILogger<LobbySlotViewModelFactory> logger,
    ILogger<LobbySlotViewModel> lobbySlotViewModelLogger,
    IScenarioImageViewModelFactory scenarioImageViewModelFactory
) : ILobbySlotViewModelFactory
{
    private readonly ILogger<LobbySlotViewModelFactory> _logger = logger;
    private readonly ILogger<LobbySlotViewModel> _lobbySlotViewModelLogger = lobbySlotViewModelLogger;
    private readonly IScenarioImageViewModelFactory _scenarioImageViewModelFactory = scenarioImageViewModelFactory;

    public LobbySlotViewModel Create(in DecodedLobbySlot initialData)
    {
        _logger.LogTrace("Creating new LobbySlotViewModel for initial data");
        return new LobbySlotViewModel(_lobbySlotViewModelLogger, _scenarioImageViewModelFactory, initialData);
    }
}
