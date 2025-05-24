using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.App.Services.TextureAtlas;
using OutbreakTracker2.Outbreak.Enums;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace OutbreakTracker2.App.Views.Common;

public class ScenarioImageViewModel : ObservableObject
{
    private readonly ILogger<ScenarioImageViewModel> _logger;
    private readonly ITextureAtlasService _textureAtlasService;

    private ImageViewModel ImageViewModel { get; }

    public CroppedBitmap? ScenarioImage => ImageViewModel.SourceImage;

    public ScenarioImageViewModel(
        ILogger<ScenarioImageViewModel> logger,
        ITextureAtlasService textureAtlasService,
        IImageViewModelFactory imageViewModelFactory)
    {
        _logger = logger;
        _textureAtlasService = textureAtlasService;
        ImageViewModel = imageViewModelFactory.Create();

        ImageViewModel.PropertyChanged += OnImageViewModelSourceImageChanged;

        _logger.LogInformation("ScenarioImageViewModel initialized");
        _ = UpdateImageAsync(Scenario.Unknown);
    }

    public ValueTask UpdateImageAsync(Scenario scenarioType)
    {
        string spriteName = _textureAtlasService.GetSpriteNameFromScenarioName(scenarioType);
        if (spriteName.StartsWith("unknown", StringComparison.Ordinal))
            return ValueTask.CompletedTask;

        _logger.LogDebug("Requesting scenario image update for scenario: {ScenarioType}", scenarioType);

        return ImageViewModel.UpdateImageAsync(spriteName, $"Scenario Image for {scenarioType}");
    }

    public ValueTask UpdateToDefaultImageAsync()
    {
        string spriteName = _textureAtlasService.GetSpriteNameFromScenarioName(Scenario.TrainingGround);
        _logger.LogDebug("Requesting scenario image update for scenario: {ScenarioType}", Scenario.TrainingGround);

        return ImageViewModel.UpdateImageAsync(spriteName, $"Scenario Image for {Scenario.TrainingGround}");
    }

    private void OnImageViewModelSourceImageChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (sender is not ImageViewModel _)
        {
            _logger.LogWarning("[{MethodName}] Unexpected sender: {Sender}",
                nameof(OnImageViewModelSourceImageChanged), sender?.GetType());
            return;
        }

        if (string.IsNullOrEmpty(eventArgs.PropertyName))
        {
            _logger.LogWarning("[{MethodName}] PropertyChangedEventArgs.PropertyName is null or empty: {PropertyName}",
                nameof(OnImageViewModelSourceImageChanged), eventArgs.PropertyName);
            return;
        }

        if (eventArgs.PropertyName.Equals(nameof(ImageViewModel.SourceImage), StringComparison.Ordinal))
            OnPropertyChanged(nameof(ScenarioImage));
    }
}