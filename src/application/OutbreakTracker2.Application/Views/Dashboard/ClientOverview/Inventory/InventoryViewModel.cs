using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Application.Utilities;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.Inventory.Factory;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Utility;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.Inventory;

public sealed partial class InventoryViewModel : ObservableObject, IDisposable
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
    [NotifyPropertyChangedFor(nameof(HasSpecialInventory))]
    [NotifyPropertyChangedFor(nameof(IsSpecialInventoryVisible))]
    [NotifyPropertyChangedFor(nameof(IsSpecialDeadInventoryVisible))]
    private string _playerStatus;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSpecialInventory))]
    [NotifyPropertyChangedFor(nameof(IsSpecialInventoryVisible))]
    [NotifyPropertyChangedFor(nameof(IsSpecialDeadInventoryVisible))]
    private string _characterType;

    public bool IsDeadOrZombie => PlayerStatus is "Dead" or "Zombie";
    public bool HasSpecialInventory => CharacterInventoryUtility.HasSpecialInventory(CharacterType);
    public bool IsSpecialInventoryVisible => HasSpecialInventory && !IsDeadOrZombie;
    public bool IsSpecialDeadInventoryVisible => HasSpecialInventory && IsDeadOrZombie;

    public InventoryViewModel(DecodedInGamePlayer player, IItemSlotViewModelFactory itemSlotViewModelFactory)
    {
        _playerStatus = player.Status;
        _characterType = player.Type;
        _itemSlotViewModelFactory = itemSlotViewModelFactory;

        EquippedItems = CreateSection(1);
        MainSlots = CreateSection(4);
        SpecialItems = CreateSection(1);
        SpecialSlots = CreateSection(4);
        DeadSlots = CreateSection(4);
        SpecialDeadSlots = CreateSection(4);
    }

    public void UpdateCharacterType(string characterType) => CharacterType = characterType;

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
        InventorySnapshot mainInventory,
        byte specialItem,
        InventorySnapshot specialInventory,
        InventorySnapshot deadInventory,
        InventorySnapshot specialDeadInventory,
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
        InventorySnapshot inventory,
        GameFile gameFile,
        DecodedItem[] scenarioItems
    )
    {
        for (int i = 0; i < slots.Length; i++)
            UpdateSlot(slots[i], inventory[i], gameFile, scenarioItems);
    }

    private void UpdateSlot(ItemSlotViewModel slot, byte itemId, GameFile gameFile, DecodedItem[] scenarioItems)
    {
        ResolvedInventorySlotValue item = InventorySlotValueResolver.Resolve(itemId, scenarioItems);
        slot.UpdateDisplay(item.Name, item.Count, item.Debug, gameFile);
    }

    public void Dispose()
    {
        DisposeSlots(EquippedItems);
        DisposeSlots(MainSlots);
        DisposeSlots(SpecialItems);
        DisposeSlots(SpecialSlots);
        DisposeSlots(DeadSlots);
        DisposeSlots(SpecialDeadSlots);
    }

    private static void DisposeSlots(IEnumerable<ItemSlotViewModel> slots)
    {
        foreach (ItemSlotViewModel slot in slots)
            slot.Dispose();
    }
}
