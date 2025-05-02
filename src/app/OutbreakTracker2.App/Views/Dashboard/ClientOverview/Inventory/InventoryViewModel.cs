using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.App.Services.Data;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.Inventory;

public partial class InventoryViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<SlotItem> _equippedItems = [];

    [ObservableProperty]
    private ObservableCollection<SlotItem> _mainSlots = [];

    [ObservableProperty]
    private ObservableCollection<SlotItem> _specialItems = [];

    [ObservableProperty]
    private ObservableCollection<SlotItem> _specialSlots = [];

    [ObservableProperty]
    private ObservableCollection<SlotItem> _deadSlots = [];

    [ObservableProperty]
    private ObservableCollection<SlotItem> _specialDeadSlots = [];

    private readonly IDataManager _dataManager;

    public InventoryViewModel(IDataManager dataManager)
    {
        _dataManager = dataManager;
        InitializeCollections();
    }

    private void InitializeCollections()
    {
        InitializeSection(EquippedItems, 1);
        InitializeSection(MainSlots, 4);
        InitializeSection(SpecialItems, 1);
        InitializeSection(SpecialSlots, 4);
        InitializeSection(DeadSlots, 4);
        InitializeSection(SpecialDeadSlots, 4);
    }

    public void UpdateFromPlayerData(
        byte equippedItem,
        byte[] mainInventory,
        byte specialItem,
        byte[] specialInventory,
        byte[] deadInventory,
        byte[] specialDeadInventory)
    {
        UpdateSlot(EquippedItems[0], equippedItem);
        UpdateSlots(MainSlots, mainInventory);
        UpdateSlot(SpecialItems[0], specialItem);
        UpdateSlots(SpecialSlots, specialInventory);
        UpdateSlots(DeadSlots, deadInventory);
        UpdateSlots(SpecialDeadSlots, specialDeadInventory);
    }

    private void UpdateSlots(ObservableCollection<SlotItem> slots, byte[] inventory)
    {
        for (int i = 0; i < slots.Count; i++)
            UpdateSlot(slots[i], inventory[i]);
    }

    private void UpdateSlot(SlotItem slot, byte itemId)
    {
        (string name, string debug) = LookupItem(itemId);
        slot.ItemName = name;
        slot.DebugInfo = debug;
    }

    private (string Name, string Debug) LookupItem(byte itemId)
    {
        if (itemId is 0x0)
            return ("Empty", "0x00 | 0");

        DecodedItem? item = _dataManager.InGameScenario?.Items?
            .FirstOrDefault(item => item.Id.Equals(itemId));

        return item is not null
            ? (item.TypeName, $"0x{itemId:X2} | {itemId}")
            : ("Unknown", $"0x{itemId:X2} | {itemId}");
    }

    private static void InitializeSection(ObservableCollection<SlotItem> collection, int count)
    {
        for (int i = 0; i < count; i++)
            collection.Add(new SlotItem
            {
                SlotNumber = i + 1,
                ItemName = "Empty",
                DebugInfo = "0x00 | 0"
            });
    }
}

public partial class SlotItem : ObservableObject
{
    [ObservableProperty]
    private int _slotNumber;

    [ObservableProperty]
    private string _itemName = string.Empty;

    [ObservableProperty]
    private string _debugInfo = string.Empty;
}