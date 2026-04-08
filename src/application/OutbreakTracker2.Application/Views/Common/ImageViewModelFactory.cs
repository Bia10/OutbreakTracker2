using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OutbreakTracker2.Application.Views.Common;

public sealed class ImageViewModelFactory(ILogger<ImageViewModelFactory> logger, IServiceProvider serviceProvider)
    : IImageViewModelFactory
{
    private readonly ILogger<ImageViewModelFactory> _logger = logger;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public ImageViewModel Create()
    {
        _logger.LogTrace("Creating a new ImageViewModel instance");

        ImageViewModel instance = _serviceProvider.GetRequiredService<ImageViewModel>();

        return instance;
    }
}
