using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Services.Atlas;
using OutbreakTracker2.Application.Services.Dispatcher;

namespace OutbreakTracker2.Application.Views.Common;

public sealed class ImageViewModelFactory(
    ILogger<ImageViewModelFactory> logger,
    ILogger<ImageViewModel> imageViewModelLogger,
    ITextureAtlasService textureAtlasService,
    IDispatcherService dispatcherService
) : IImageViewModelFactory
{
    private readonly ILogger<ImageViewModelFactory> _logger = logger;
    private readonly ILogger<ImageViewModel> _imageViewModelLogger = imageViewModelLogger;
    private readonly ITextureAtlasService _textureAtlasService = textureAtlasService;
    private readonly IDispatcherService _dispatcherService = dispatcherService;

    public ImageViewModel Create()
    {
        _logger.LogTrace("Creating a new ImageViewModel instance");
        return new ImageViewModel(_imageViewModelLogger, _textureAtlasService, _dispatcherService);
    }
}
