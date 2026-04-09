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

    private static (int SlotId, Ulid Id) GetEnemyKey(DecodedEnemy enemy) => (enemy.SlotId, enemy.Id);

    public void UpdateItems(DecodedItem[] newItems, int frameCounter, GameFile gameFile)
    {
        List<DecodedItem> newItemsList = [.. newItems.Where(item => !IsUnoccupiedSlot(item))];
        HashSet<byte> newSlotIndices = [.. newItemsList.Select(item => item.SlotIndex)];
        Dictionary<byte, ScenarioItemSlotViewModel> existingBySlot = _items.ToDictionary(vm => vm.SlotIndex);

        for (int i = _items.Count - 1; i >= 0; i--)
        {
            ScenarioItemSlotViewModel existingSlotVm = _items[i];
            if (!newSlotIndices.Contains(existingSlotVm.SlotIndex))
            {
                _previousPickedUpStates.Remove(existingSlotVm.SlotIndex);
                _items.RemoveAt(i);
                existingBySlot.Remove(existingSlotVm.SlotIndex);
            }
        }

        foreach (DecodedItem newItem in newItemsList)
        {
            existingBySlot.TryGetValue(newItem.SlotIndex, out ScenarioItemSlotViewModel? existingSlotVm);

            if (existingSlotVm is null)
            {
                ItemImageViewModel imageVm = _itemImageViewModelFactory.Create();
                ScenarioItemSlotViewModel newSlotVm = new(newItem, imageVm, gameFile);
                _items.Add(newSlotVm);
                existingBySlot[newItem.SlotIndex] = newSlotVm;
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
        HashSet<(int SlotId, Ulid Id)> newEnemyKeys = [.. newEnemiesList.Select(GetEnemyKey)];

        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            DecodedEnemy existingEnemy = _enemies[i];
            if (!newEnemyKeys.Contains(GetEnemyKey(existingEnemy)))
                _enemies.RemoveAt(i);
        }

        Dictionary<(int SlotId, Ulid Id), int> existingIndexByKey = _enemies
            .Select((enemy, index) => (Key: GetEnemyKey(enemy), Index: index))
            .ToDictionary(entry => entry.Key, entry => entry.Index);

        foreach (DecodedEnemy newEnemy in newEnemiesList)
        {
            (int SlotId, Ulid Id) key = GetEnemyKey(newEnemy);

            if (!existingIndexByKey.TryGetValue(key, out int existingIndex))
            {
                _enemies.Add(newEnemy);
                existingIndexByKey[key] = _enemies.Count - 1;
            }
            else
            {
                DecodedEnemy existingEnemy = _enemies[existingIndex];

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
                    _enemies[existingIndex] = newEnemy;
            }
        }
    }

    public void UpdateDoors(DecodedDoor[] newDoors)
    {
        List<DecodedDoor> newDoorsList = [.. newDoors];
        HashSet<Ulid> newDoorIds = [.. newDoorsList.Select(door => door.Id)];
        Dictionary<Ulid, InGameDoorViewModel> existingById = _doors.ToDictionary(vm => vm.UniqueId);

        for (int i = _doors.Count - 1; i >= 0; i--)
        {
            InGameDoorViewModel existingVm = _doors[i];
            if (!newDoorIds.Contains(existingVm.UniqueId))
            {
                _doors.RemoveAt(i);
                existingById.Remove(existingVm.UniqueId);
            }
        }

        foreach (DecodedDoor newDoor in newDoorsList)
        {
            existingById.TryGetValue(newDoor.Id, out InGameDoorViewModel? existingVm);

            if (existingVm is null)
            {
                InGameDoorViewModel newVm = new(newDoor);
                _doors.Add(newVm);
                existingById[newDoor.Id] = newVm;
            }
            else
                existingVm.Update(newDoor);
        }
    }
}
