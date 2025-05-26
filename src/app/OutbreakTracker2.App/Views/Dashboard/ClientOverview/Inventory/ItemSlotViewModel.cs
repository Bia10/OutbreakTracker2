using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.App.Views.Common.Item;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Utility;
using System;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.Inventory;

public partial class ItemSlotViewModel : ObservableObject
{
    private readonly ILogger<ItemSlotViewModel> _logger;

    public ItemImageViewModel ItemImageViewModel { get; }

    [ObservableProperty]
    private int _slotNumber;

    [ObservableProperty]
    private string _itemName = string.Empty;

    [ObservableProperty]
    private string _itemCount = string.Empty;

    [ObservableProperty]
    private string _debugInfo = string.Empty;

    public ItemSlotViewModel(
        ILogger<ItemSlotViewModel> logger,
        IItemImageViewModelFactory itemImageViewModelFactory)
    {
        _logger = logger;
        ItemImageViewModel = itemImageViewModelFactory.Create();
    }

    public void UpdateDisplay(string name, string count, string debug)
    {
        ItemName = name;
        ItemCount = count;
        DebugInfo = debug;

if (!name.Equals("Empty", StringComparison.Ordinal))
        {
            string currentFile = EnumUtility.GetEnumString(GameFile.FileTwo, GameFile.Unknown);
            string itemSpriteLookupName = currentFile + "/" + name;

            _ = ItemImageViewModel.UpdateImageAsync(itemSpriteLookupName);
        }

        _logger.LogInformation("ItemSlotViewModel updated for item: {ItemName}", name);
    }

    public void ClearDisplay()
    {
        ItemName = string.Empty;
        DebugInfo = string.Empty;

        _logger.LogInformation("ItemSlotViewModel cleared");
    }
}