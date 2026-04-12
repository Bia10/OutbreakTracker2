using Avalonia;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Services.Atlas;
using OutbreakTracker2.Application.Services.Dispatcher;
using OutbreakTracker2.Application.Utilities;

namespace OutbreakTracker2.Application.Views.Common;

public sealed partial class ImageViewModel : ObservableObject
{
    private readonly ILogger<ImageViewModel> _logger;
    private readonly IDispatcherService _dispatcherService;
    private readonly ITextureAtlas? _uiAtlas;
    private readonly ITextureAtlas? _itemsAtlas;

    [ObservableProperty]
    private CroppedBitmap? _sourceImage;

    public ImageViewModel(
        ILogger<ImageViewModel> logger,
        ITextureAtlasService textureAtlasService,
        IDispatcherService dispatcherService
    )
    {
        _logger = logger;
        _dispatcherService = dispatcherService;

        _uiAtlas = textureAtlasService.GetAtlas(AtlasName.UI);
        if (_uiAtlas is null)
            _logger.LogWarning(
                "UI Texture Atlas is unavailable during ImageViewModel construction; images will remain empty until atlases are loaded."
            );

        _itemsAtlas = textureAtlasService.GetAtlas(AtlasName.Items);
        if (_itemsAtlas is null)
            _logger.LogWarning(
                "Items Texture Atlas is unavailable during ImageViewModel construction; images will remain empty until atlases are loaded."
            );
    }

    public async ValueTask ClearImageAsync()
    {
        await _dispatcherService
            .InvokeOnUIAsync(() =>
            {
                SourceImage = null;
            })
            .ConfigureAwait(true);
    }

    public async ValueTask UpdateImageAsync(string spriteName, string debugContext = "")
    {
        bool isItemSprite = spriteName.StartsWith("File", StringComparison.Ordinal);
        ITextureAtlas? selectedAtlas = isItemSprite ? _itemsAtlas : _uiAtlas;

        if (selectedAtlas is null)
        {
            _logger.LogWarning(
                "Cannot update image because the {AtlasName} texture atlas is unavailable. Context: {DebugContext}",
                isItemSprite ? AtlasName.Items : AtlasName.UI,
                debugContext
            );

            await ClearImageAsync().ConfigureAwait(true);
            return;
        }

        CroppedBitmap? newImage = null;

        Rect sourceRect = selectedAtlas.GetSourceRectangle(spriteName);
        if (sourceRect.Width <= 0 || sourceRect.Height <= 0)
        {
            _logger.LogWarning(
                "Sprite '{SpriteName}' not found in atlas or rectangle is invalid for context '{DebugContext}'. Using fallback image",
                spriteName,
                debugContext
            );
        }
        else
        {
            if (selectedAtlas.Texture is null)
            {
                _logger.LogWarning(
                    "Texture atlas texture is null. Cannot create CroppedBitmap for sprite '{SpriteName}'. Context: {DebugContext}",
                    spriteName,
                    debugContext
                );
                return;
            }

            try
            {
                // Note: tbh not rly sure why CroppedBitmap is not thread safe, but it seems to be the case
                // i.e. why does a CroppedBitmap needs to be created on the UI thread?
                newImage = await _dispatcherService
                    .InvokeOnUIAsync(() => ImageUtility.GetCroppedBitmap(selectedAtlas.Texture, sourceRect))
                    .ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to create CroppedBitmap for sprite '{SpriteName}' on background thread. Context: {DebugContext}",
                    spriteName,
                    debugContext
                );
            }
        }

        await _dispatcherService
            .InvokeOnUIAsync(() =>
            {
                SourceImage = newImage;
            })
            .ConfigureAwait(true);
    }
}
