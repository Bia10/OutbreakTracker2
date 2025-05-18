using CommunityToolkit.Mvvm.ComponentModel;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.Inventory;

public partial class ItemSlotViewModel : ObservableObject
{
    [ObservableProperty]
    private int _slotNumber;

    [ObservableProperty]
    private string _itemName = string.Empty;

    [ObservableProperty]
    private string _debugInfo = string.Empty;

    public void UpdateDisplay(string name, string debug)
    {
        ItemName = name;
        DebugInfo = debug;
    }
}