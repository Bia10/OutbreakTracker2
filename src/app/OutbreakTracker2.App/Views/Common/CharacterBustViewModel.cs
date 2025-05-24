using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.App.Services.TextureAtlas;
using OutbreakTracker2.Outbreak.Enums.Character;
using System.Threading.Tasks;

namespace OutbreakTracker2.App.Views.Common;

public class CharacterBustViewModel : ObservableObject
{
    private readonly ILogger<CharacterBustViewModel> _logger;
    private readonly ITextureAtlasService _textureAtlasService;

    private ImageViewModel ImageViewModel { get; }

    public CharacterBustViewModel(
        ILogger<CharacterBustViewModel> logger,
        ITextureAtlasService textureAtlasService,
        IImageViewModelFactory imageViewModelFactory)
    {
        _logger = logger;
        _textureAtlasService = textureAtlasService;
        ImageViewModel = imageViewModelFactory.Create();

        _logger.LogDebug("CharacterBustViewModel initialized");
        _ = UpdateBustAsync(CharacterBaseType.Kevin);
    }

    public ValueTask UpdateBustAsync(CharacterBaseType characterType)
    {
        string spriteName = _textureAtlasService.GetSpriteNameFromCharacterType(characterType);
        _logger.LogDebug("Requesting bust update for character: {CharacterType}", characterType);

        return ImageViewModel.UpdateImageAsync(spriteName, $"Character Bust for {characterType}");
    }

    // Todo: this won't automatically notify the UI when the image changes
    public CroppedBitmap? PlayerBust => ImageViewModel.SourceImage;
}