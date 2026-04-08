using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Application.Services.Toasts;
using OutbreakTracker2.Application.Views.Common.Item;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameDoor;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.Entitites;

public partial class ScenarioEntitiesViewModel : ObservableObject
{
    private readonly IToastService _toastService;
    private readonly IItemImageViewModelFactory _itemImageViewModelFactory;
    private readonly Dictionary<byte, short> _previousPickedUpStates = [];

    [ObservableProperty]
    private ObservableCollection<ScenarioItemSlotViewModel> _items = [];

    [ObservableProperty]
    private ObservableCollection<DecodedEnemy> _enemies = [];

    [ObservableProperty]
    private ObservableCollection<InGameDoorViewModel> _doors = [];

    public ScenarioEntitiesViewModel(IToastService toastService, IItemImageViewModelFactory itemImageViewModelFactory)
    {
        _toastService = toastService;
        _itemImageViewModelFactory = itemImageViewModelFactory;
    }

    private static bool IsUnoccupiedSlot(DecodedItem item) =>
        item.SlotIndex == 0 && item.Quantity == 0 && item.PickedUp == 0 && item.Present == 0;

    public void UpdateItems(DecodedItem[] newItems, int frameCounter)
    {
        List<DecodedItem> newItemsList = [.. newItems.Where(item => !IsUnoccupiedSlot(item))];

        for (int i = Items.Count - 1; i >= 0; i--)
        {
            ScenarioItemSlotViewModel existingSlotVm = Items[i];
            if (!newItemsList.Exists(newItem => newItem.SlotIndex == existingSlotVm.SlotIndex))
            {
                _previousPickedUpStates.Remove(existingSlotVm.SlotIndex);
                Items.RemoveAt(i);
            }
        }

        foreach (DecodedItem newItem in newItemsList)
        {
            ScenarioItemSlotViewModel? existingSlotVm = Items.FirstOrDefault(vm => vm.SlotIndex == newItem.SlotIndex);

            if (existingSlotVm is null)
            {
                ItemImageViewModel imageVm = _itemImageViewModelFactory.Create();
                Items.Add(new ScenarioItemSlotViewModel(newItem, imageVm));
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
                    }
                }

                _previousPickedUpStates[newItem.SlotIndex] = newItem.PickedUp;
                existingSlotVm.UpdateItem(newItem, frameCounter);
            }
        }
    }

    public void UpdateEnemies(DecodedEnemy[] newEnemies)
    {
        List<DecodedEnemy> newEnemiesList = [.. newEnemies];

        for (int i = Enemies.Count - 1; i >= 0; i--)
        {
            DecodedEnemy existingEnemy = Enemies[i];
            if (
                !newEnemiesList.Exists(newEnemy =>
                    newEnemy.SlotId == existingEnemy.SlotId && newEnemy.Id == existingEnemy.Id
                )
            )
                Enemies.RemoveAt(i);
        }

        foreach (DecodedEnemy newEnemy in newEnemiesList)
        {
            DecodedEnemy? existingEnemy = Enemies.FirstOrDefault(e =>
                e.SlotId == newEnemy.SlotId && e.Id == newEnemy.Id
            );

            if (existingEnemy is null)
                Enemies.Add(newEnemy);
            else
            {
                int index = Enemies.IndexOf(existingEnemy);
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
                    Enemies[index] = newEnemy;
                }
            }
        }
    }

    public void UpdateDoors(DecodedDoor[] newDoors)
    {
        List<DecodedDoor> newDoorsList = [.. newDoors];

        for (int i = Doors.Count - 1; i >= 0; i--)
        {
            InGameDoorViewModel existingVm = Doors[i];
            if (newDoorsList.TrueForAll(newDoor => newDoor.Id != existingVm.UniqueId))
                Doors.RemoveAt(i);
        }

        foreach (DecodedDoor newDoor in newDoorsList)
        {
            InGameDoorViewModel? existingVm = Doors.FirstOrDefault(vm => vm.UniqueId == newDoor.Id);

            if (existingVm is null)
                Doors.Add(new InGameDoorViewModel(newDoor));
            else
                existingVm.Update(newDoor);
        }
    }
}
