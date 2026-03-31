using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Views.Common.ScenarioImg;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.LobbySlot.Factory;

public class LobbySlotViewModelFactory(ILogger<LobbySlotViewModelFactory> logger, IServiceProvider serviceProvider)
    : ILobbySlotViewModelFactory
{
    private readonly ILogger<LobbySlotViewModelFactory> _logger = logger;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public LobbySlotViewModel Create(DecodedLobbySlot initialData)
    {
        _logger.LogDebug("Creating new LobbySlotViewModel for initial data");

        ILogger<LobbySlotViewModel> lobbySlotVmLogger = _serviceProvider.GetRequiredService<
            ILogger<LobbySlotViewModel>
        >();
        IScenarioImageViewModelFactory scenarioImageVmFactory =
            _serviceProvider.GetRequiredService<IScenarioImageViewModelFactory>();

        return new LobbySlotViewModel(lobbySlotVmLogger, scenarioImageVmFactory, initialData);
    }
}
