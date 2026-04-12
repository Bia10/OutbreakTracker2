using System.Globalization;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OutbreakTracker2.Application.Views.Common.Item;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Utility;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.Entities;

public sealed partial class ScenarioItemSlotViewModel : ObservableObject, IItemSlotDisplay
{
    private static readonly Color SlotChangedGlowColor = Colors.Orange;
    private static readonly Color SlotEmptiedGlowColor = Colors.Red;

    public ItemImageViewModel ItemImageViewModel { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TypeName))]
    [NotifyPropertyChangedFor(nameof(Quantity))]
    [NotifyPropertyChangedFor(nameof(QuantityText))]
    [NotifyPropertyChangedFor(nameof(SlotIndex))]
    [NotifyPropertyChangedFor(nameof(PickedUp))]
    [NotifyPropertyChangedFor(nameof(PickedUpByName))]
    [NotifyPropertyChangedFor(nameof(RoomName))]
    [NotifyPropertyChangedFor(nameof(RoomId))]
    [NotifyPropertyChangedFor(nameof(Mix))]
    [NotifyPropertyChangedFor(nameof(Present))]
    [NotifyPropertyChangedFor(nameof(PickedUpDisplay))]
    [NotifyPropertyChangedFor(nameof(IsHeldByPlayer))]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    [NotifyPropertyChangedFor(nameof(DebugInfo))]
    private DecodedItem _item;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TrackingMenuHeader))]
    private bool _isPickupTracked;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PickedUpAtDisplay))]
    private int? _pickedUpAtFrame;

    public byte PositionIndex { get; private set; }

    public string TypeName => Item.TypeName;
    public short Quantity => Item.Quantity;
    public byte SlotIndex => Item.SlotIndex;
    public short PickedUp => Item.PickedUp;
    public string PickedUpByName => Item.PickedUpByName;
    public string RoomName => Item.RoomName;
    public byte RoomId => Item.RoomId;
    public byte Mix => Item.Mix;
    public int Present => Item.Present;

    private bool UsesInventoryEmptyStyle => Item is { TypeId: 0, Quantity: 0, PickedUp: 0, Present: 0 };

    public bool IsEmpty =>
        UsesInventoryEmptyStyle
        || string.IsNullOrEmpty(Item.TypeName)
        || string.Equals(Item.TypeName, "Unknown", StringComparison.Ordinal);
    public string DisplayName => IsEmpty ? "Empty" : Item.TypeName;
    public string QuantityText => Quantity.ToString(CultureInfo.InvariantCulture);
    public int SlotNumber => PositionIndex;
    public string DebugInfo => UsesInventoryEmptyStyle ? "0x00 | 0" : $"0x{Item.Id:X2} | {Item.Id}";

    public string PickedUpDisplay => Item.PickedUp > 0 ? $"P{Item.PickedUp}" : string.Empty;
    public bool IsHeldByPlayer => Item.PickedUp > 0;
    public string TrackingMenuHeader => IsPickupTracked ? "Stop Tracking Pickup" : "Track Pickup";
    public string PickedUpAtDisplay =>
        PickedUpAtFrame.HasValue ? TimeUtility.GetTimeFromFrames(PickedUpAtFrame.Value) : "--:--:--.–";

    public event EventHandler<GlowEventArgs>? GlowTriggered;

    public ScenarioItemSlotViewModel(
        DecodedItem item,
        ItemImageViewModel itemImageViewModel,
        GameFile gameFile,
        byte positionIndex
    )
    {
        _item = item;
        ItemImageViewModel = itemImageViewModel;
        PositionIndex = positionIndex;
        RefreshImage(gameFile);
    }

    [RelayCommand]
    private void TogglePickupTracking() => IsPickupTracked = !IsPickupTracked;

    public void UpdateItem(in DecodedItem newItem, int frameCounter, GameFile gameFile, byte positionIndex)
    {
        Color? glowColor = DetermineGlowColor(Item, newItem);

        if (Item.PickedUp == 0 && newItem.PickedUp > 0)
            PickedUpAtFrame = frameCounter;
        else if (newItem.PickedUp == 0)
            PickedUpAtFrame = null;

        if (glowColor.HasValue)
            GlowTriggered?.Invoke(this, new GlowEventArgs(glowColor.Value));

        bool wasEmpty = IsEmpty;
        string previousTypeName = TypeName;

        Item = newItem;
        PositionIndex = positionIndex;

        if (IsEmpty != wasEmpty || !string.Equals(TypeName, previousTypeName, StringComparison.Ordinal))
            RefreshImage(gameFile);
    }

    private static Color? DetermineGlowColor(in DecodedItem previousItem, in DecodedItem newItem)
    {
        bool wasEmpty = IsDisplayEmpty(previousItem);
        bool isEmpty = IsDisplayEmpty(newItem);

        if (!wasEmpty && isEmpty)
            return SlotEmptiedGlowColor;

        if (isEmpty)
            return null;

        if (
            wasEmpty
            || previousItem.Id != newItem.Id
            || previousItem.TypeId != newItem.TypeId
            || previousItem.Quantity != newItem.Quantity
            || previousItem.Mix != newItem.Mix
            || !string.Equals(previousItem.TypeName, newItem.TypeName, StringComparison.Ordinal)
        )
            return SlotChangedGlowColor;

        return null;
    }

    private static bool IsDisplayEmpty(in DecodedItem item) =>
        item is { TypeId: 0, Quantity: 0, PickedUp: 0, Present: 0 }
        || string.IsNullOrEmpty(item.TypeName)
        || string.Equals(item.TypeName, "Unknown", StringComparison.Ordinal);

    private void RefreshImage(GameFile gameFile)
    {
        if (IsEmpty || string.IsNullOrEmpty(Item.TypeName))
        {
            _ = ItemImageViewModel.ClearImageAsync();
            return;
        }

        string currentFile = EnumUtility.GetEnumString(gameFile, GameFile.Unknown);
        string spriteLookupName = currentFile + "/" + Item.TypeName;
        _ = ItemImageViewModel.UpdateImageAsync(spriteLookupName);
    }
}
