using CommunityToolkit.Mvvm.ComponentModel;
using ObservableCollections;
using OutbreakTracker2.Application.Services.Toasts;
using OutbreakTracker2.Application.Views.Common.Item;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameDoor;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.Entitites;

public sealed partial class ScenarioEntitiesViewModel : ObservableObject, IDisposable
{
    private readonly IToastService _toastService;
    private readonly IItemImageViewModelFactory _itemImageViewModelFactory;
    private readonly Dictionary<byte, short> _previousPickedUpStates = [];

    private readonly ObservableList<ScenarioItemSlotViewModel> _items = new();
    private readonly ObservableList<DecodedEnemy> _enemies = new();
    private readonly ObservableList<InGameDoorViewModel> _doors = new();
    private readonly ObservableList<ScenarioRoomGroupViewModel> _roomGroups = new();

    public NotifyCollectionChangedSynchronizedViewList<ScenarioItemSlotViewModel> Items { get; }
    public NotifyCollectionChangedSynchronizedViewList<DecodedEnemy> Enemies { get; }
    public NotifyCollectionChangedSynchronizedViewList<InGameDoorViewModel> Doors { get; }
    public NotifyCollectionChangedSynchronizedViewList<ScenarioRoomGroupViewModel> RoomGroups { get; }

    [ObservableProperty]
    private bool _hasRoomGroups;

    public ScenarioEntitiesViewModel(IToastService toastService, IItemImageViewModelFactory itemImageViewModelFactory)
    {
        _toastService = toastService;
        _itemImageViewModelFactory = itemImageViewModelFactory;

        Items = _items.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
        Enemies = _enemies.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
        Doors = _doors.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
        RoomGroups = _roomGroups.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
    }

    public void Dispose()
    {
        Items.Dispose();
        Enemies.Dispose();
        Doors.Dispose();
        RoomGroups.Dispose();
    }

    public void ClearItems()
    {
        _items.Clear();
        _roomGroups.Clear();
        _previousPickedUpStates.Clear();
        HasRoomGroups = false;
    }

    public void UpdateItems(DecodedItem[] newItems, int frameCounter, GameFile gameFile)
    {
        // First call: retain the raw 255-slot decode, then project a filtered room-group view.
        if (_items.Count == 0)
        {
            ScenarioItemSlotViewModel[] vms = new ScenarioItemSlotViewModel[newItems.Length];
            for (int i = 0; i < newItems.Length; i++)
            {
                ItemImageViewModel imageVm = _itemImageViewModelFactory.Create();
                vms[i] = new ScenarioItemSlotViewModel(newItems[i], imageVm, gameFile, (byte)i);
                _previousPickedUpStates[(byte)i] = newItems[i].PickedUp;
            }
            _items.AddRange(vms);

            HashSet<int> visibleIndices = [.. ScenarioItemRoomGroupProjection.GetVisibleIndices(newItems)];
            List<ScenarioItemSlotViewModel> visibleItems = [];
            for (int i = 0; i < vms.Length; i++)
            {
                if (visibleIndices.Contains(i))
                    visibleItems.Add(vms[i]);
            }

            ScenarioRoomGroupViewModel[] groups = visibleItems
                .GroupBy(vm => string.IsNullOrEmpty(vm.RoomName) ? "Unknown" : vm.RoomName)
                .OrderBy(g => string.Equals(g.Key, "Spawning/Scenario Cleared", StringComparison.Ordinal) ? 1 : 0)
                .ThenBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => new ScenarioRoomGroupViewModel(g.Key, g.ToList()))
                .ToArray();
            _roomGroups.AddRange(groups);
            HasRoomGroups = groups.Length > 0;
            return;
        }

        // Subsequent calls: update every slot in-place — no add/remove, only Replace events.
        for (int i = 0; i < newItems.Length && i < _items.Count; i++)
        {
            DecodedItem newItem = newItems[i];
            ScenarioItemSlotViewModel vm = _items[i];
            byte slotKey = (byte)i;

            if (vm.IsPickupTracked && !string.IsNullOrEmpty(newItem.TypeName))
            {
                short previousPickedUp = _previousPickedUpStates.GetValueOrDefault(slotKey, (short)0);
                if (previousPickedUp == 0 && newItem.PickedUp > 0)
                {
                    string holder =
                        string.IsNullOrEmpty(newItem.PickedUpByName)
                        || string.Equals(newItem.PickedUpByName, "None", StringComparison.Ordinal)
                            ? $"P{newItem.PickedUp}"
                            : newItem.PickedUpByName;
                    _ = _toastService.InvokeInfoToastAsync($"{holder} picked up {newItem.TypeName}", "Item Picked Up");
                    vm.IsPickupTracked = false;
                }
            }

            _previousPickedUpStates[slotKey] = newItem.PickedUp;
            vm.UpdateItem(newItem, frameCounter, gameFile, slotKey);
        }
    }

    public void UpdateEnemies(DecodedEnemy[] newEnemies)
    {
        // Batch reset: Clear fires a single Reset event, AddRange fires a single Add event.
        // This avoids individual Remove events which trigger DataGrid virtualization
        // RemoveNonDisplayedRows during a measure pass, causing an ArgumentOutOfRangeException.
        _enemies.Clear();
        _enemies.AddRange(newEnemies);
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
