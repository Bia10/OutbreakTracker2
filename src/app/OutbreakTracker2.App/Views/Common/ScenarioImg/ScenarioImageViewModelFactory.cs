using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace OutbreakTracker2.App.Views.Common.ScenarioImg;

public class ScenarioImageViewModelFactory : IScenarioImageViewModelFactory
{
    private readonly ILogger<ScenarioImageViewModelFactory> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ScenarioImageViewModelFactory(
        ILogger<ScenarioImageViewModelFactory> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public ScenarioImageViewModel Create()
    {
        _logger.LogDebug("Creating a new ScenarioImageViewModel instance");

        ScenarioImageViewModel newScenarioImageViewModel =
            _serviceProvider.GetRequiredService<ScenarioImageViewModel>();

        _logger.LogDebug("ScenarioImageViewModel created successfully");
        return newScenarioImageViewModel;
    }
}