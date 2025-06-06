using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace OutbreakTracker2.Application.Views.Common.Item;

public class ItemImageViewModelFactory : IItemImageViewModelFactory
{
    private readonly ILogger<ItemImageViewModelFactory> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ItemImageViewModelFactory(
        ILogger<ItemImageViewModelFactory> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public ItemImageViewModel Create()
    {
        _logger.LogDebug("Creating a new ItemImageViewModel instance");

        ItemImageViewModel newItemImageViewModel =
            _serviceProvider.GetRequiredService<ItemImageViewModel>();

        _logger.LogDebug("ItemImageViewModel created successfully");
        return newItemImageViewModel;
    }
}