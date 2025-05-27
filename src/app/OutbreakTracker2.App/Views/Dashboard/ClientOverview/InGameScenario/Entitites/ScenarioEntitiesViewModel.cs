using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Outbreak.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGameScenario.Entitites;

public partial class ScenarioEntitiesViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DecodedItem> _items = [];

    [ObservableProperty]
    private ObservableCollection<DecodedEnemy> _enemies = [];

    public void UpdateItems(DecodedItem[] newItems)
    {
        List<DecodedItem> newItemsList = newItems.ToList();

        for (int i = Items.Count - 1; i >= 0; i--)
        {
            DecodedItem existingItem = Items[i];
            if (!newItemsList.Any(newItem => newItem.SlotIndex == existingItem.SlotIndex && newItem.Id == existingItem.Id))
                Items.RemoveAt(i);
        }

        foreach (DecodedItem newItem in newItemsList)
        {
            DecodedItem? existingItem = Items.FirstOrDefault(existingItem => existingItem.SlotIndex == newItem.SlotIndex && existingItem.Id == newItem.Id);
            if (existingItem == null)
                Items.Add(newItem);
            else
            {
                int index = Items.IndexOf(existingItem);
                if (index is -1)
                    continue;

                if (existingItem.En != newItem.En ||
                    existingItem.TypeName != newItem.TypeName ||
                    existingItem.Quantity != newItem.Quantity ||
                    existingItem.PickedUp != newItem.PickedUp ||
                    existingItem.Present != newItem.Present ||
                    existingItem.Mix != newItem.Mix ||
                    existingItem.RoomId != newItem.RoomId)
                {
                    Items[index] = newItem;
                }
            }
        }
    }

    public void UpdateEnemies(DecodedEnemy[] newEnemies)
    {
        List<DecodedEnemy> newEnemiesList = newEnemies.ToList();

        for (int i = Enemies.Count - 1; i >= 0; i--)
        {
            DecodedEnemy existingEnemy = Enemies[i];
            if (!newEnemiesList.Any(newEnemy => newEnemy.SlotId == existingEnemy.SlotId && newEnemy.Id == existingEnemy.Id))
                Enemies.RemoveAt(i);
        }

        foreach (DecodedEnemy newEnemy in newEnemiesList)
        {
            DecodedEnemy? existingEnemy = Enemies.FirstOrDefault(e => e.SlotId == newEnemy.SlotId && e.Id == newEnemy.Id);

            if (existingEnemy is null)
            {
                Enemies.Add(newEnemy);
            }
            else
            {
                int index = Enemies.IndexOf(existingEnemy);
                if (index is -1)
                    continue;

                if (existingEnemy.Enabled != newEnemy.Enabled ||
                    existingEnemy.InGame != newEnemy.InGame ||
                    existingEnemy.RoomId != newEnemy.RoomId ||
                    existingEnemy.TypeId != newEnemy.TypeId ||
                    existingEnemy.NameId != newEnemy.NameId ||
                    existingEnemy.Name != newEnemy.Name ||
                    existingEnemy.CurHp != newEnemy.CurHp ||
                    existingEnemy.MaxHp != newEnemy.MaxHp ||
                    existingEnemy.BossType != newEnemy.BossType ||
                    existingEnemy.Status != newEnemy.Status ||
                    existingEnemy.RoomName != newEnemy.RoomName)
                {
                    Enemies[index] = newEnemy;
                }
            }
        }
    }
}