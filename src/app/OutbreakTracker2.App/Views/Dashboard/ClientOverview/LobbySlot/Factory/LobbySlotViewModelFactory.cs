using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.App.Views.Common.ScenarioImg;
using OutbreakTracker2.Outbreak.Models;
using System;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.LobbySlot.Factory;

public class LobbySlotViewModelFactory : ILobbySlotViewModelFactory
{
    private readonly ILogger<LobbySlotViewModelFactory> _logger;
    private readonly IServiceProvider _serviceProvider;

    public LobbySlotViewModelFactory(
        ILogger<LobbySlotViewModelFactory> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public LobbySlotViewModel Create(DecodedLobbySlot initialData)
    {
        _logger.LogDebug("Creating new LobbySlotViewModel for initial data");

        ILogger<LobbySlotViewModel> lobbySlotVmLogger =
            _serviceProvider.GetRequiredService<ILogger<LobbySlotViewModel>>();
        IScenarioImageViewModelFactory scenarioImageVmFactory =
            _serviceProvider.GetRequiredService<IScenarioImageViewModelFactory>();

        return new LobbySlotViewModel(lobbySlotVmLogger, scenarioImageVmFactory, initialData);
    }
}