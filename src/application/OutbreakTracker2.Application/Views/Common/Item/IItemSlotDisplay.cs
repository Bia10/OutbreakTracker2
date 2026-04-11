using System.ComponentModel;

namespace OutbreakTracker2.Application.Views.Common.Item;

public interface IItemSlotDisplay : INotifyPropertyChanged
{
    ItemImageViewModel ItemImageViewModel { get; }
    string DisplayName { get; }
    string QuantityText { get; }
    int SlotNumber { get; }
    string DebugInfo { get; }
    bool IsEmpty { get; }
}
