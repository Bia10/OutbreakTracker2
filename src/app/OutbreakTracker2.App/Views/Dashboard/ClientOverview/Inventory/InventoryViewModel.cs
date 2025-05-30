using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.App.Services.Data;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.Inventory.Factory;
using OutbreakTracker2.Outbreak.Models;
using System.Collections.ObjectModel;
using ZLinq;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.Inventory;

public partial class InventoryViewModel : ObservableObject
{
    private readonly IDataManager _dataManager;
    private readonly IItemSlotViewModelFactory _itemSlotViewModelFactory;

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
    [NotifyPropertyChangedFor(nameof(IsSpecialInventoryVisible))]
    [NotifyPropertyChangedFor(nameof(IsSpecialDeadInventoryVisible))]
    private string _playerStatus;

    [ObservableProperty]
    private string _playerName;

    public bool IsDeadOrZombie => PlayerStatus is "Dead" or "Zombie";
    public bool HasSpecialInventory => PlayerName is "Yoko" or "Cindy" or "David";
    public bool IsSpecialInventoryVisible => HasSpecialInventory && !IsDeadOrZombie;
    public bool IsSpecialDeadInventoryVisible => HasSpecialInventory && IsDeadOrZombie;

    public InventoryViewModel(
        DecodedInGamePlayer player,
        IDataManager dataManager,
        IItemSlotViewModelFactory itemSlotViewModelFactory)
    {
        _playerStatus = player.Status;
        _playerName = player.Name;
        _dataManager = dataManager;
        _itemSlotViewModelFactory = itemSlotViewModelFactory;

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
        (string name, string count, string debug) = LookupItemDetails(itemId);
        slot.UpdateDisplay(name, count, debug);
    }

    private (string Name, string Count, string Debug) LookupItemDetails(byte itemId)
    {
        if (itemId is 0x0)
            return ("Empty", "0", "0x00 | 0");

        DecodedItem? item = _dataManager.InGameScenario.Items
            .AsValueEnumerable()
            .Where(IsValidItem)
            .FirstOrDefault(item => item.Id.Equals(itemId));

        return item is not null
            ? (item.TypeName, item.Quantity.ToString(), $"0x{itemId:X2} | {itemId}")
            : ("Unknown", "0", $"0x{itemId:X2} | {itemId}");
    }

    private void InitializeSection(ObservableCollection<ItemSlotViewModel> collection, int count)
    {
        for (int i = 0; i < count; i++)
        {
            ItemSlotViewModel slotItem = _itemSlotViewModelFactory.Create(i + 1);
            collection.Add(slotItem);
        }
    }

    private static bool IsValidItem(DecodedItem item)
        => item is not { SlotIndex: 0, PickedUp: 0 };
}