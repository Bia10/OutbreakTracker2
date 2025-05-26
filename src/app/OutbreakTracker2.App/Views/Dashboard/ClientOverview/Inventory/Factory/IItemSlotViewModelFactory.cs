namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.Inventory.Factory;

public interface IItemSlotViewModelFactory
{
    ItemSlotViewModel Create(int slotNumber);
}