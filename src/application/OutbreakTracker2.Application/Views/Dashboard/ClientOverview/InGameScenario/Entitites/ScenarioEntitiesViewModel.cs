using ObservableCollections;
using OutbreakTracker2.Application.Services.Toasts;
using OutbreakTracker2.Application.Views.Common.Item;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameDoor;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.Entitites;

public sealed class ScenarioEntitiesViewModel : IDisposable
{
    private readonly IToastService _toastService;
    private readonly IItemImageViewModelFactory _itemImageViewModelFactory;
    private readonly Dictionary<byte, short> _previousPickedUpStates = [];

    private readonly ObservableList<ScenarioItemSlotViewModel> _items = new();
    private readonly ObservableList<DecodedEnemy> _enemies = new();
    private readonly ObservableList<InGameDoorViewModel> _doors = new();

    public NotifyCollectionChangedSynchronizedViewList<ScenarioItemSlotViewModel> Items { get; }
    public NotifyCollectionChangedSynchronizedViewList<DecodedEnemy> Enemies { get; }
    public NotifyCollectionChangedSynchronizedViewList<InGameDoorViewModel> Doors { get; }

    public ScenarioEntitiesViewModel(IToastService toastService, IItemImageViewModelFactory itemImageViewModelFactory)
    {
        _toastService = toastService;
        _itemImageViewModelFactory = itemImageViewModelFactory;

        Items = _items.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
        Enemies = _enemies.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
        Doors = _doors.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
    }

    public void Dispose()
    {
        Items.Dispose();
        Enemies.Dispose();
        Doors.Dispose();
    }

    private static bool IsUnoccupiedSlot(DecodedItem item) =>
        item.SlotIndex == 0 && item.Quantity == 0 && item.PickedUp == 0 && item.Present == 0;

    public void UpdateItems(DecodedItem[] newItems, int frameCounter, GameFile gameFile)
    {
        List<DecodedItem> newItemsList = [.. newItems.Where(item => !IsUnoccupiedSlot(item))];

        for (int i = _items.Count - 1; i >= 0; i--)
        {
            ScenarioItemSlotViewModel existingSlotVm = _items[i];
            if (!newItemsList.Exists(newItem => newItem.SlotIndex == existingSlotVm.SlotIndex))
            {
                _previousPickedUpStates.Remove(existingSlotVm.SlotIndex);
                _items.RemoveAt(i);
            }
        }

        foreach (DecodedItem newItem in newItemsList)
        {
            ScenarioItemSlotViewModel? existingSlotVm = _items.FirstOrDefault(vm => vm.SlotIndex == newItem.SlotIndex);

            if (existingSlotVm is null)
            {
                ItemImageViewModel imageVm = _itemImageViewModelFactory.Create();
                _items.Add(new ScenarioItemSlotViewModel(newItem, imageVm, gameFile));
                _previousPickedUpStates[newItem.SlotIndex] = newItem.PickedUp;
            }
            else
            {
                if (existingSlotVm.IsPickupTracked)
                {
                    short previousPickedUp = _previousPickedUpStates.GetValueOrDefault(newItem.SlotIndex, (short)0);
                    if (previousPickedUp == 0 && newItem.PickedUp > 0)
                    {
                        string holder =
                            string.IsNullOrEmpty(newItem.PickedUpByName)
                            || string.Equals(newItem.PickedUpByName, "None", StringComparison.Ordinal)
                                ? $"P{newItem.PickedUp}"
                                : newItem.PickedUpByName;
                        _ = _toastService.InvokeInfoToastAsync(
                            $"{holder} picked up {newItem.TypeName}",
                            "Item Picked Up"
                        );
                        existingSlotVm.IsPickupTracked = false;
                    }
                }

                _previousPickedUpStates[newItem.SlotIndex] = newItem.PickedUp;
                existingSlotVm.UpdateItem(newItem, frameCounter, gameFile);
            }
        }
    }

    public void UpdateEnemies(DecodedEnemy[] newEnemies)
    {
        List<DecodedEnemy> newEnemiesList = [.. newEnemies];

        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            DecodedEnemy existingEnemy = _enemies[i];
            if (
                !newEnemiesList.Exists(newEnemy =>
                    newEnemy.SlotId == existingEnemy.SlotId && newEnemy.Id == existingEnemy.Id
                )
            )
                _enemies.RemoveAt(i);
        }

        foreach (DecodedEnemy newEnemy in newEnemiesList)
        {
            DecodedEnemy? existingEnemy = _enemies.FirstOrDefault(e =>
                e.SlotId == newEnemy.SlotId && e.Id == newEnemy.Id
            );

            if (existingEnemy is null)
                _enemies.Add(newEnemy);
            else
            {
                int index = _enemies.IndexOf(existingEnemy);
                if (index is -1)
                    continue;

                if (
                    existingEnemy.Enabled != newEnemy.Enabled
                    || existingEnemy.InGame != newEnemy.InGame
                    || existingEnemy.RoomId != newEnemy.RoomId
                    || existingEnemy.TypeId != newEnemy.TypeId
                    || existingEnemy.NameId != newEnemy.NameId
                    || !string.Equals(existingEnemy.Name, newEnemy.Name, System.StringComparison.Ordinal)
                    || existingEnemy.CurHp != newEnemy.CurHp
                    || existingEnemy.MaxHp != newEnemy.MaxHp
                    || existingEnemy.BossType != newEnemy.BossType
                    || existingEnemy.Status != newEnemy.Status
                    || !string.Equals(existingEnemy.RoomName, newEnemy.RoomName, System.StringComparison.Ordinal)
                )
                {
                    _enemies[index] = newEnemy;
                }
            }
        }
    }

    public void UpdateDoors(DecodedDoor[] newDoors)
    {
        List<DecodedDoor> newDoorsList = [.. newDoors];

        for (int i = _doors.Count - 1; i >= 0; i--)
        {
            InGameDoorViewModel existingVm = _doors[i];
            if (newDoorsList.TrueForAll(newDoor => newDoor.Id != existingVm.UniqueId))
                _doors.RemoveAt(i);
        }

        foreach (DecodedDoor newDoor in newDoorsList)
        {
            InGameDoorViewModel? existingVm = _doors.FirstOrDefault(vm => vm.UniqueId == newDoor.Id);

            if (existingVm is null)
                _doors.Add(new InGameDoorViewModel(newDoor));
            else
                existingVm.Update(newDoor);
        }
    }
}
