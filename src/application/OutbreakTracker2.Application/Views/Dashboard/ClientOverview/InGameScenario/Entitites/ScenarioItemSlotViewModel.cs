using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OutbreakTracker2.Application.Views.Common.Item;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Utility;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.Entitites;

public sealed partial class ScenarioItemSlotViewModel : ObservableObject
{
    public ItemImageViewModel ItemImageViewModel { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TypeName))]
    [NotifyPropertyChangedFor(nameof(Quantity))]
    [NotifyPropertyChangedFor(nameof(SlotIndex))]
    [NotifyPropertyChangedFor(nameof(PickedUp))]
    [NotifyPropertyChangedFor(nameof(PickedUpByName))]
    [NotifyPropertyChangedFor(nameof(RoomName))]
    [NotifyPropertyChangedFor(nameof(Mix))]
    [NotifyPropertyChangedFor(nameof(Present))]
    [NotifyPropertyChangedFor(nameof(PickedUpDisplay))]
    [NotifyPropertyChangedFor(nameof(IsHeldByPlayer))]
    private DecodedItem _item;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TrackingMenuHeader))]
    private bool _isPickupTracked;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PickedUpAtDisplay))]
    private int? _pickedUpAtFrame;

    public string TypeName => Item.TypeName;
    public short Quantity => Item.Quantity;
    public byte SlotIndex => Item.SlotIndex;
    public short PickedUp => Item.PickedUp;
    public string PickedUpByName => Item.PickedUpByName;
    public string RoomName => Item.RoomName;
    public byte Mix => Item.Mix;
    public int Present => Item.Present;

    public string PickedUpDisplay => Item.PickedUp > 0 ? $"P{Item.PickedUp}" : string.Empty;
    public bool IsHeldByPlayer => Item.PickedUp > 0;
    public string TrackingMenuHeader => IsPickupTracked ? "Stop Tracking Pickup" : "Track Pickup";
    public string PickedUpAtDisplay =>
        PickedUpAtFrame.HasValue ? TimeUtility.GetTimeFromFrames(PickedUpAtFrame.Value) : "--:--:--.–";

    public ScenarioItemSlotViewModel(DecodedItem item, ItemImageViewModel itemImageViewModel, GameFile gameFile)
    {
        _item = item;
        ItemImageViewModel = itemImageViewModel;
        RefreshImage(gameFile);
    }

    [RelayCommand]
    private void TogglePickupTracking() => IsPickupTracked = !IsPickupTracked;

    public void UpdateItem(DecodedItem newItem, int frameCounter, GameFile gameFile)
    {
        if (Item.PickedUp == 0 && newItem.PickedUp > 0)
            PickedUpAtFrame = frameCounter;
        else if (newItem.PickedUp == 0)
            PickedUpAtFrame = null;

        Item = newItem;
        RefreshImage(gameFile);
    }

    private void RefreshImage(GameFile gameFile)
    {
        if (string.IsNullOrEmpty(Item.TypeName))
        {
            _ = ItemImageViewModel.ClearImageAsync();
            return;
        }

        string currentFile = EnumUtility.GetEnumString(gameFile, GameFile.Unknown);
        string spriteLookupName = currentFile + "/" + Item.TypeName;
        _ = ItemImageViewModel.UpdateImageAsync(spriteLookupName);
    }
}
