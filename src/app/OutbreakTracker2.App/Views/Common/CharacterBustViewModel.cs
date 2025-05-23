using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.App.Services.Dispatcher;
using OutbreakTracker2.App.Services.TextureAtlas;
using System;
using System.Threading.Tasks;

namespace OutbreakTracker2.App.Views.Common;

public partial class CharacterBustViewModel : ObservableObject
{
    private readonly ILogger<CharacterBustViewModel> _logger;
    private readonly IDispatcherService _dispatcherService;
    private readonly ITextureAtlas? _uiAtlas;

    [ObservableProperty]
    private CroppedBitmap? _playerBust;

    public CharacterBustViewModel(
        ILogger<CharacterBustViewModel> logger,
        ITextureAtlasService textureAtlasService,
        IDispatcherService dispatcherService)
    {
        _logger = logger;
        _dispatcherService = dispatcherService;

        // assumes we have atlas loaded already
        _uiAtlas = textureAtlasService.GetAtlas("UI");
        if (_uiAtlas is null)
        {
            _logger.LogError("UI Texture Atlas could not be retrieved by CharacterBustViewModel");
            throw new InvalidOperationException("UI Texture Atlas could not be retrieved by CharacterBustViewModel");
        }

        _ = UpdateBustAsync("bustKevin");
    }

    public async Task UpdateBustAsync(string characterTypeName)
    {
        if (string.IsNullOrEmpty(characterTypeName))
        {
_logger.LogError("Unable to update bust for null or empty characterTypeName '{CharacterTypeName}'", characterTypeName);
            return;
        }

        string spriteName = GetSpriteNameFromCharacterType(characterTypeName);
        if (_uiAtlas is not null)
        {
            Rect sourceRect = _uiAtlas.GetSourceRectangle(spriteName);

            CroppedBitmap? newBust = null;
            if (sourceRect.Width <= 0 || sourceRect.Height <= 0)
                _logger.LogWarning(
                    "Sprite '{SpriteName}' not found in atlas for character type '{CharacterTypeName}' or rectangle is invalid. Using fallback image",
                    spriteName, characterTypeName);
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

    private string GetSpriteNameFromCharacterType(string characterType)
    {
        string normalizedType = characterType.ToLowerInvariant().Trim();
        _logger.LogDebug("Translating character type '{CharacterType}' to sprite name", characterType);

        string spriteName = normalizedType switch
        {
            "alyssa" => "bustAlyssa",
            "cindy" => "bustCindy",
            "david" => "bustDavid",
            "george" => "bustGeorge",
            "jim" => "bustJim",
            "kevin" => "bustKevin",
            "mark" => "bustMark",
            "yoko" => "bustYoko",
            _ when normalizedType.StartsWith("bust", StringComparison.Ordinal) => characterType,
            _ => "bustUnknown"
        };

        _logger.LogDebug("Obtained sprite name '{SpriteName}'", spriteName);
        return spriteName;
    }
}