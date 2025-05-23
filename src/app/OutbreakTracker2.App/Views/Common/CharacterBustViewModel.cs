using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.App.Services.Dispatcher;
using OutbreakTracker2.App.Services.TextureAtlas;
using OutbreakTracker2.Outbreak.Enums.Character;
using System;
using System.Threading.Tasks;

namespace OutbreakTracker2.App.Views.Common;

public partial class CharacterBustViewModel : ObservableObject
{
    private readonly ILogger<CharacterBustViewModel> _logger;
    private readonly IDispatcherService _dispatcherService;
    private readonly ITextureAtlasService _textureAtlasService;
    private readonly ITextureAtlas? _uiAtlas;

    [ObservableProperty]
    private CroppedBitmap? _playerBust;

    public CharacterBustViewModel(
        ILogger<CharacterBustViewModel> logger,
        ITextureAtlasService textureAtlasService,
        IDispatcherService dispatcherService)
    {
        _logger = logger;
        _textureAtlasService = textureAtlasService;
        _dispatcherService = dispatcherService;

        // assumes we have atlas loaded already
        _uiAtlas = _textureAtlasService.GetAtlas("UI");
        if (_uiAtlas is null)
        {
            _logger.LogError("UI Texture Atlas could not be retrieved by CharacterBustViewModel");
            throw new InvalidOperationException("UI Texture Atlas could not be retrieved by CharacterBustViewModel");
        }

        _ = UpdateBustAsync(CharacterBaseType.Kevin);
    }

    public async Task UpdateBustAsync(CharacterBaseType characterType)
    {
        string spriteName = _textureAtlasService.GetSpriteNameFromCharacterType(characterType);

        if (_uiAtlas is not null)
        {
            Rect sourceRect = _uiAtlas.GetSourceRectangle(spriteName);

            CroppedBitmap? newBust = null;
            if (sourceRect.Width <= 0 || sourceRect.Height <= 0)
            {
                _logger.LogWarning(
                    "Sprite '{SpriteName}' not found in atlas for character type '{CharacterType}' or rectangle is invalid. Using fallback image",
                    spriteName, characterType);
            }
            else
            {
                if (_uiAtlas.Texture is null)
                {
                    _logger.LogWarning("Texture atlas texture is null. Cannot create CroppedBitmap for sprite '{SpriteName}'", spriteName);
                    return;
                }

                try
                {
                    newBust = await _dispatcherService.InvokeOnUIAsync(() => GetCroppedBitmap(_uiAtlas.Texture, sourceRect));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create CroppedBitmap for sprite '{SpriteName}' on UI thread", spriteName);
                }
            }

            await _dispatcherService.InvokeOnUIAsync(() =>
            {
                PlayerBust = newBust;
            });
        }
    }

    private static CroppedBitmap GetCroppedBitmap(IImage imageSource, Rect rectSource)
    {
        PixelRect pixelRect = new((int)rectSource.X, (int)rectSource.Y, (int)rectSource.Width, (int)rectSource.Height);
        return new CroppedBitmap(imageSource, pixelRect);
    }
}