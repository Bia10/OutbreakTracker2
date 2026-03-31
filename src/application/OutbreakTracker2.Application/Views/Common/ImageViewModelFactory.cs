using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OutbreakTracker2.Application.Views.Common;

public class ImageViewModelFactory(ILogger<ImageViewModelFactory> logger, IServiceProvider serviceProvider)
    : IImageViewModelFactory
{
    private readonly ILogger<ImageViewModelFactory> _logger = logger;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public ImageViewModel Create()
    {
        _logger.LogDebug("Creating a new ImageViewModel instance");

        ImageViewModel instance = _serviceProvider.GetRequiredService<ImageViewModel>();

        _logger.LogDebug("Created a new ImageViewModel instance successfully");
        return instance;
    }
}
