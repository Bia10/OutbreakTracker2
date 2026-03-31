using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OutbreakTracker2.Application.Views.Common.ScenarioImg;

public class ScenarioImageViewModelFactory(
    ILogger<ScenarioImageViewModelFactory> logger,
    IServiceProvider serviceProvider
) : IScenarioImageViewModelFactory
{
    private readonly ILogger<ScenarioImageViewModelFactory> _logger = logger;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public ScenarioImageViewModel Create()
    {
        _logger.LogDebug("Creating a new ScenarioImageViewModel instance");

        ScenarioImageViewModel newScenarioImageViewModel =
            _serviceProvider.GetRequiredService<ScenarioImageViewModel>();

        _logger.LogDebug("ScenarioImageViewModel created successfully");
        return newScenarioImageViewModel;
    }
}
