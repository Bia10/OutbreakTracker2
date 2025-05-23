using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.App.Services.TextureAtlas;
using OutbreakTracker2.Outbreak.Enums;
using System.Threading.Tasks;

namespace OutbreakTracker2.App.Views.Common;

public class ScenarioImageViewModel : ObservableObject
{
    private readonly ILogger<ScenarioImageViewModel> _logger;
    private readonly ITextureAtlasService _textureAtlasService;

    private ImageViewModel ImageViewModel { get; }

    public ScenarioImageViewModel(
        ILogger<ScenarioImageViewModel> logger,
        ITextureAtlasService textureAtlasService,
        IImageViewModelFactory imageViewModelFactory)
    {
        _logger = logger;
        _textureAtlasService = textureAtlasService;
        ImageViewModel = imageViewModelFactory.Create();

        _logger.LogInformation("ScenarioImageViewModel initialized");
        _ = UpdateScenarioImageAsync(Scenario.Unknown);
    }

    public ValueTask UpdateScenarioImageAsync(Scenario scenarioType)
    {
        string spriteName = _textureAtlasService.GetSpriteNameFromScenarioName(scenarioType);
        _logger.LogDebug("Requesting scenario image update for scenario: {ScenarioType}", scenarioType);

        return ImageViewModel.UpdateImageAsync(spriteName, $"Scenario Image for {scenarioType}");
    }

    public CroppedBitmap? CurrentScenarioImage => ImageViewModel.SourceImage;
}