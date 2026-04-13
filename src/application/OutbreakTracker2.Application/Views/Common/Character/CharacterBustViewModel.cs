using System.ComponentModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Services.Atlas;
using OutbreakTracker2.Outbreak.Enums.Character;

namespace OutbreakTracker2.Application.Views.Common.Character;

/// <summary>
/// Wraps bust-image loading for a character card. Dispose the view model when its owner is removed
/// so the inner image-view-model property-changed subscription is released.
/// </summary>
public sealed class CharacterBustViewModel : ObservableObject, IDisposable
{
    private readonly ILogger<CharacterBustViewModel> _logger;
    private readonly ISpriteNameResolver _spriteNameResolver;

    private ImageViewModel ImageViewModel { get; }

    public CroppedBitmap? PlayerBustImage => ImageViewModel.SourceImage;

    public CharacterBustViewModel(
        ILogger<CharacterBustViewModel> logger,
        ISpriteNameResolver spriteNameResolver,
        IImageViewModelFactory imageViewModelFactory
    )
    {
        _logger = logger;
        _spriteNameResolver = spriteNameResolver;
        ImageViewModel = imageViewModelFactory.Create();

        ImageViewModel.PropertyChanged += OnImageViewModelSourceImageChanged;

        _ = UpdateBustAsync(CharacterBaseType.Kevin);
    }

    public void Dispose() => ImageViewModel.PropertyChanged -= OnImageViewModelSourceImageChanged;

    private CharacterBaseType? _currentCharacterType;

    public ValueTask UpdateBustAsync(CharacterBaseType characterType)
    {
        if (_currentCharacterType == characterType)
            return ValueTask.CompletedTask;

        _currentCharacterType = characterType;
        string spriteName = _spriteNameResolver.GetSpriteNameFromCharacterType(characterType);
        _logger.LogTrace("Requesting bust update for character: {CharacterType}", characterType);

        return ImageViewModel.UpdateImageAsync(spriteName, $"Character Bust for {characterType}");
    }

    private void OnImageViewModelSourceImageChanged(object? sender, PropertyChangedEventArgs e)
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

        if (string.IsNullOrEmpty(e.PropertyName))
        {
            _logger.LogWarning(
                "[{MethodName}] PropertyChangedEventArgs is null or empty: {PropertyName}",
                nameof(OnImageViewModelSourceImageChanged),
                e.PropertyName
            );
            return;
        }

        if (
            e.PropertyName.Equals(
                nameof(OutbreakTracker2.Application.Views.Common.ImageViewModel.SourceImage),
                StringComparison.Ordinal
            )
        )
            OnPropertyChanged(nameof(PlayerBustImage));
    }
}
