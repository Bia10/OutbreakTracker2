using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace OutbreakTracker2.App.Views.Common;

public class ImageViewModelFactory : IImageViewModelFactory
{
    private readonly ILogger<ImageViewModelFactory> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ImageViewModelFactory(
        ILogger<ImageViewModelFactory> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public ImageViewModel Create()
    {
        _logger.LogDebug("Creating a new ImageViewModel instance");

        ImageViewModel instance =
            _serviceProvider.GetRequiredService<ImageViewModel>();

        _logger.LogDebug("Created a new ImageViewModel instance successfully");
        return instance;
    }
}