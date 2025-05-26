using Microsoft.Extensions.Logging;
using OutbreakTracker2.App.Views.Common.Item;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.Inventory.Factory;

public class ItemSlotViewModelFactory : IItemSlotViewModelFactory
{
    private readonly ILogger<ItemSlotViewModel> _logger;
    private readonly IItemImageViewModelFactory _itemImageViewModelFactory;

    public ItemSlotViewModelFactory(
        ILogger<ItemSlotViewModel> logger,
        IItemImageViewModelFactory itemImageViewModelFactory)
    {
        _logger = logger;
        _itemImageViewModelFactory = itemImageViewModelFactory;
    }

    public ItemSlotViewModel Create(int slotNumber)
    {
        ItemSlotViewModel itemSlotViewModel = new(_logger, _itemImageViewModelFactory)
        {
            SlotNumber = slotNumber,
            ItemName = "Empty",
            DebugInfo = "0x00 | 0"
        };

        return itemSlotViewModel;
    }
}