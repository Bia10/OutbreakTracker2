using Microsoft.Extensions.Logging;

namespace OutbreakTracker2.Application.Views.Common.Item;

public sealed class ItemImageViewModelFactory(
    ILogger<ItemImageViewModelFactory> logger,
    ILogger<ItemImageViewModel> itemImageViewModelLogger,
    IImageViewModelFactory imageViewModelFactory
) : IItemImageViewModelFactory
{
    private readonly ILogger<ItemImageViewModelFactory> _logger = logger;
    private readonly ILogger<ItemImageViewModel> _itemImageViewModelLogger = itemImageViewModelLogger;
    private readonly IImageViewModelFactory _imageViewModelFactory = imageViewModelFactory;

    public ItemImageViewModel Create()
    {
        _logger.LogTrace("Creating a new ItemImageViewModel instance");
        return new ItemImageViewModel(_itemImageViewModelLogger, _imageViewModelFactory);
    }
}
