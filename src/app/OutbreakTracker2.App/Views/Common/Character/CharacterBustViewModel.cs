using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.App.Services.TextureAtlas;
using OutbreakTracker2.Outbreak.Enums.Character;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace OutbreakTracker2.App.Views.Common.Character;

public class CharacterBustViewModel : ObservableObject
{
    private readonly ILogger<CharacterBustViewModel> _logger;
    private readonly ITextureAtlasService _textureAtlasService;

    private ImageViewModel ImageViewModel { get; }

    public CroppedBitmap? PlayerBustImage => ImageViewModel.SourceImage;

    public CharacterBustViewModel(
        ILogger<CharacterBustViewModel> logger,
        ITextureAtlasService textureAtlasService,
        IImageViewModelFactory imageViewModelFactory)
    {
        _logger = logger;
        _textureAtlasService = textureAtlasService;
        ImageViewModel = imageViewModelFactory.Create();

        ImageViewModel.PropertyChanged += OnImageViewModelSourceImageChanged;

        _logger.LogDebug("CharacterBustViewModel initialized");
        _ = UpdateBustAsync(CharacterBaseType.Kevin);
    }

    public ValueTask UpdateBustAsync(CharacterBaseType characterType)
    {
        string spriteName = _textureAtlasService.GetSpriteNameFromCharacterType(characterType);
        _logger.LogDebug("Requesting bust update for character: {CharacterType}", characterType);

        return ImageViewModel.UpdateImageAsync(spriteName, $"Character Bust for {characterType}");
    }

    private void OnImageViewModelSourceImageChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not ImageViewModel _)
        {
            _logger.LogWarning("[{MethodName}] Unexpected sender: {Sender}",
                nameof(OnImageViewModelSourceImageChanged), sender?.GetType());
            return;
        }

        if (string.IsNullOrEmpty(e.PropertyName))
        {
            _logger.LogWarning("[{MethodName}] PropertyChangedEventArgs is null or empty: {PropertyName}",
                nameof(OnImageViewModelSourceImageChanged), e.PropertyName);
            return;
        }

        if (e.PropertyName.Equals(nameof(ImageViewModel.SourceImage), StringComparison.Ordinal))
            OnPropertyChanged(nameof(PlayerBustImage));
    }
}