using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Services.Atlas;

namespace OutbreakTracker2.Application.Views.Common.ScenarioImg;

public sealed class ScenarioImageViewModelFactory(
    ILogger<ScenarioImageViewModelFactory> logger,
    ILogger<ScenarioImageViewModel> scenarioImageViewModelLogger,
    ISpriteNameResolver spriteNameResolver,
    IImageViewModelFactory imageViewModelFactory
) : IScenarioImageViewModelFactory
{
    private readonly ILogger<ScenarioImageViewModelFactory> _logger = logger;
    private readonly ILogger<ScenarioImageViewModel> _scenarioImageViewModelLogger = scenarioImageViewModelLogger;
    private readonly ISpriteNameResolver _spriteNameResolver = spriteNameResolver;
    private readonly IImageViewModelFactory _imageViewModelFactory = imageViewModelFactory;

    public ScenarioImageViewModel Create()
    {
        _logger.LogTrace("Creating a new ScenarioImageViewModel instance");
        return new ScenarioImageViewModel(_scenarioImageViewModelLogger, _spriteNameResolver, _imageViewModelFactory);
    }
}
