using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.Inventory.Factory;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.Inventory;

public sealed partial class InventoryViewModel : ObservableObject
{
    private readonly IItemSlotViewModelFactory _itemSlotViewModelFactory;

    public ItemSlotViewModel[] EquippedItems { get; }
    public ItemSlotViewModel[] MainSlots { get; }
    public ItemSlotViewModel[] SpecialItems { get; }
    public ItemSlotViewModel[] SpecialSlots { get; }
    public ItemSlotViewModel[] DeadSlots { get; }
    public ItemSlotViewModel[] SpecialDeadSlots { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDeadOrZombie))]
    [NotifyPropertyChangedFor(nameof(IsSpecialInventoryVisible))]
    [NotifyPropertyChangedFor(nameof(IsSpecialDeadInventoryVisible))]
    private string _playerStatus;

    [ObservableProperty]
    private string _playerName;

    public bool IsDeadOrZombie => PlayerStatus is "Dead" or "Zombie";
    public bool HasSpecialInventory => PlayerName is "Yoko" or "Cindy" or "David";
    public bool IsSpecialInventoryVisible => HasSpecialInventory && !IsDeadOrZombie;
    public bool IsSpecialDeadInventoryVisible => HasSpecialInventory && IsDeadOrZombie;

    public InventoryViewModel(DecodedInGamePlayer player, IItemSlotViewModelFactory itemSlotViewModelFactory)
    {
        _playerStatus = player.Status;
        _playerName = player.Name;
        _itemSlotViewModelFactory = itemSlotViewModelFactory;

        EquippedItems = CreateSection(1);
        MainSlots = CreateSection(4);
        SpecialItems = CreateSection(1);
        SpecialSlots = CreateSection(4);
        DeadSlots = CreateSection(4);
        SpecialDeadSlots = CreateSection(4);
    }

    private ItemSlotViewModel[] CreateSection(int count)
    {
        ItemSlotViewModel[] slots = new ItemSlotViewModel[count];
        for (int i = 0; i < count; i++)
            slots[i] = _itemSlotViewModelFactory.Create(i + 1);
        return slots;
    }

    public void UpdateFromPlayerData(
        string playerStatus,
        byte equippedItem,
        byte[] mainInventory,
        byte specialItem,
        byte[] specialInventory,
        byte[] deadInventory,
        byte[] specialDeadInventory,
        GameFile gameFile,
        DecodedItem[] scenarioItems
    )
    {
        PlayerStatus = playerStatus;
        UpdateSlot(EquippedItems[0], equippedItem, gameFile, scenarioItems);
        UpdateSlots(MainSlots, mainInventory, gameFile, scenarioItems);
        UpdateSlot(SpecialItems[0], specialItem, gameFile, scenarioItems);
        UpdateSlots(SpecialSlots, specialInventory, gameFile, scenarioItems);
        UpdateSlots(DeadSlots, deadInventory, gameFile, scenarioItems);
        UpdateSlots(SpecialDeadSlots, specialDeadInventory, gameFile, scenarioItems);
    }

    private void UpdateSlots(
        ItemSlotViewModel[] slots,
        byte[] inventory,
        GameFile gameFile,
        DecodedItem[] scenarioItems
    )
    {
        for (int i = 0; i < slots.Length; i++)
            UpdateSlot(slots[i], inventory[i], gameFile, scenarioItems);
    }

    private void UpdateSlot(ItemSlotViewModel slot, byte itemId, GameFile gameFile, DecodedItem[] scenarioItems)
    {
        (string name, string count, string debug) = LookupItemDetails(itemId, scenarioItems);
        slot.UpdateDisplay(name, count, debug, gameFile);
    }

    private static (string Name, string Count, string Debug) LookupItemDetails(byte itemId, DecodedItem[] scenarioItems)
    {
        if (itemId is 0x0)
            return ("Empty", "0", "0x00 | 0");

        DecodedItem? item = scenarioItems
            .AsValueEnumerable()
            .Where(IsValidItem)
            .FirstOrDefault(item => item.Id.Equals(itemId));

        return item is { } i
            ? (i.TypeName, i.Quantity.ToString(CultureInfo.InvariantCulture), $"0x{itemId:X2} | {itemId}")
            : ("Unknown", "0", $"0x{itemId:X2} | {itemId}");
    }

    private static bool IsValidItem(DecodedItem item) => item is not { SlotIndex: 0, PickedUp: 0 };
}
