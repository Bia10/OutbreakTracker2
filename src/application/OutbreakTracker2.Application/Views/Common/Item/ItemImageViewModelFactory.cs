using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OutbreakTracker2.Application.Views.Common.Item;

public sealed class ItemImageViewModelFactory(
    ILogger<ItemImageViewModelFactory> logger,
    IServiceProvider serviceProvider
) : IItemImageViewModelFactory
{
    private readonly ILogger<ItemImageViewModelFactory> _logger = logger;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public ItemImageViewModel Create()
    {
        _logger.LogTrace("Creating a new ItemImageViewModel instance");

        ItemImageViewModel newItemImageViewModel = _serviceProvider.GetRequiredService<ItemImageViewModel>();

        return newItemImageViewModel;
    }
}
