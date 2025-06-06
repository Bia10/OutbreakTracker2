namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.Inventory.Factory;

public interface IItemSlotViewModelFactory
{
    ItemSlotViewModel Create(int slotNumber);
}