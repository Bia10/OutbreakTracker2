using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.App.Services.Data;
using OutbreakTracker2.Outbreak.Models;
using System.Collections.ObjectModel;
using ZLinq;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.Inventory;

public partial class InventoryViewModel : ObservableObject
{
    private readonly IDataManager _dataManager;

    [ObservableProperty]
    private ObservableCollection<ItemSlotViewModel> _equippedItems = [];

    [ObservableProperty]
    private ObservableCollection<ItemSlotViewModel> _mainSlots = [];

    [ObservableProperty]
    private ObservableCollection<ItemSlotViewModel> _specialItems = [];

    [ObservableProperty]
    private ObservableCollection<ItemSlotViewModel> _specialSlots = [];

    [ObservableProperty]
    private ObservableCollection<ItemSlotViewModel> _deadSlots = [];

    [ObservableProperty]
    private ObservableCollection<ItemSlotViewModel> _specialDeadSlots = [];

    [ObservableProperty]
    private string _playerStatus;

    [ObservableProperty]
    private string _playerName;

    public bool IsDeadOrZombie => PlayerStatus is "Dead" or "Zombie";
    public bool HasSpecialInventory => PlayerName is "Yoko" or "Cindy" or "David";
    public bool HasSpecialDeadInventory => IsDeadOrZombie && HasSpecialInventory;

    public InventoryViewModel(DecodedInGamePlayer player, IDataManager dataManager)
    {
        _playerStatus = player.Status;
        _playerName = player.CharacterName;
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

    private void UpdateSlots(ObservableCollection<ItemSlotViewModel> slots, byte[] inventory)
    {
        for (int i = 0; i < slots.Count; i++)
            UpdateSlot(slots[i], inventory[i]);
    }

    private void UpdateSlot(ItemSlotViewModel slot, byte itemId)
    {
        (string name, string debug) = LookupItem(itemId);
        slot.ItemName = name;
        slot.DebugInfo = debug;
    }

    private (string Name, string Debug) LookupItem(byte itemId)
    {
        if (itemId is 0x0)
            return ("Empty", "0x00 | 0");

        DecodedItem? item = _dataManager.InGameScenario.Items
            .AsValueEnumerable()
            .Where(IsValidItem)
            .FirstOrDefault(item => item.Id.Equals(itemId));

        return item is not null
            ? (item.TypeName, $"0x{itemId:X2} | {itemId}")
            : ("Unknown", $"0x{itemId:X2} | {itemId}");
    }

    private static void InitializeSection(ObservableCollection<ItemSlotViewModel> collection, int count)
    {
        for (int i = 0; i < count; i++)
        {
            ItemSlotViewModel slotItem = new()
            {
                SlotNumber = i + 1,
                ItemName = "Empty",
                DebugInfo = "0x00 | 0"
            };

            collection.Add(slotItem);
        }
    }

    // Note: Item slotIndex 0 implies that item is not spawned on map
    // Item pickedUp 0 implies its not inside player inventory
    // as such, for practical matters item which is neither on map nor in inventory is invalid
    private static bool IsValidItem(DecodedItem item)
        => item is not { SlotIndex: 0, PickedUp: 0 };
}