using System.ComponentModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Services.Atlas;
using OutbreakTracker2.Outbreak.Enums;

namespace OutbreakTracker2.Application.Views.Common.ScenarioImg;

public sealed class ScenarioImageViewModel : ObservableObject
{
    private readonly ILogger<ScenarioImageViewModel> _logger;
    private readonly ISpriteNameResolver _spriteNameResolver;

    private ImageViewModel ImageViewModel { get; }

    public CroppedBitmap? ScenarioImage => ImageViewModel.SourceImage;

    public ScenarioImageViewModel(
        ILogger<ScenarioImageViewModel> logger,
        ISpriteNameResolver spriteNameResolver,
        IImageViewModelFactory imageViewModelFactory
    )
    {
        _logger = logger;
        _spriteNameResolver = spriteNameResolver;
        ImageViewModel = imageViewModelFactory.Create();

        ImageViewModel.PropertyChanged += OnImageViewModelSourceImageChanged;

        _ = UpdateImageAsync(Scenario.Unknown);
    }

    public ValueTask UpdateImageAsync(Scenario scenarioType)
    {
        string spriteName = _spriteNameResolver.GetSpriteNameFromScenarioName(scenarioType);
        if (spriteName.StartsWith("unknown", StringComparison.Ordinal))
            return ValueTask.CompletedTask;

        _logger.LogTrace("Requesting scenario image update for scenario: {ScenarioType}", scenarioType);

        return ImageViewModel.UpdateImageAsync(spriteName, $"Scenario Image for {scenarioType}");
    }

    public ValueTask UpdateToDefaultImageAsync()
    {
        string spriteName = _spriteNameResolver.GetSpriteNameFromScenarioName(Scenario.TrainingGround);
        _logger.LogTrace("Requesting scenario image update for scenario: {ScenarioType}", Scenario.TrainingGround);

        return ImageViewModel.UpdateImageAsync(spriteName, $"Scenario Image for {nameof(Scenario.TrainingGround)}");
    }

    private void OnImageViewModelSourceImageChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (sender is not ImageViewModel _)
        {
            _logger.LogWarning(
                "[{MethodName}] Unexpected sender: {Sender}",
                nameof(OnImageViewModelSourceImageChanged),
                sender?.GetType()
            );
            return;
        }

        if (string.IsNullOrEmpty(eventArgs.PropertyName))
        {
            _logger.LogWarning(
                "[{MethodName}] PropertyChangedEventArgs.PropertyName is null or empty: {PropertyName}",
                nameof(OnImageViewModelSourceImageChanged),
                eventArgs.PropertyName
            );
            return;
        }

        if (
            eventArgs.PropertyName.Equals(
                nameof(OutbreakTracker2.Application.Views.Common.ImageViewModel.SourceImage),
                StringComparison.Ordinal
            )
        )
            OnPropertyChanged(nameof(ScenarioImage));
    }
}
