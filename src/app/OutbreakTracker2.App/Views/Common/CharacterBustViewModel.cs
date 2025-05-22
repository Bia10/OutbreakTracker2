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
    private readonly ITextureAtlas _textureAtlas;
    private readonly IDispatcherService _dispatcherService;

    [ObservableProperty]
    private CroppedBitmap? _playerBust;

    public CharacterBustViewModel(
        ILogger<CharacterBustViewModel> logger,
        ITextureAtlas textureAtlas,
        IDispatcherService dispatcherService)
    {
        _textureAtlas = textureAtlas;
        _logger = logger;
        _dispatcherService = dispatcherService;
        _ = UpdateBustAsync("bustKevin");
    }

    public async Task UpdateBustAsync(string characterTypeName)
    {
        string spriteName = GetSpriteNameFromCharacterType(characterTypeName);
        Rect sourceRect = _textureAtlas.GetSourceRectangle(spriteName);

        CroppedBitmap? newBust = null;
        if (sourceRect == new Rect(0, 0, 0, 0) || sourceRect.Width <= 0 || sourceRect.Height <= 0)
            _logger.LogWarning(
                "Sprite '{SpriteName}' not found in atlas for character type '{CharacterTypeName}' or rectangle is invalid. Using fallback image",
                spriteName, characterTypeName);
        else
        {
            if (_textureAtlas.Texture is null)
            {
                _logger.LogWarning("Texture atlas is null. Cannot create CroppedBitmap for sprite '{SpriteName}'", spriteName);
                return;
            }

            try
            {
                newBust = await _dispatcherService.InvokeOnUIAsync(() => GetCroppedBitmap(_textureAtlas.Texture, sourceRect));
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

    private static CroppedBitmap GetCroppedBitmap(IImage imageSource, Rect rectSource)
    {
        PixelRect pixelRect = new((int)rectSource.X, (int)rectSource.Y, (int)rectSource.Width, (int)rectSource.Height);
        return new CroppedBitmap(imageSource, pixelRect);
    }

    private static string GetSpriteNameFromCharacterType(string characterType)
    {
        string normalizedType = characterType.ToLowerInvariant().Trim();
        return normalizedType switch
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
    }
}