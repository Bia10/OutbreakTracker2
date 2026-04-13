using System.Globalization;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OutbreakTracker2.Application.Views.Common.Item;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Utility;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.Entities;

public sealed partial class ScenarioItemSlotViewModel : ObservableObject, IItemSlotDisplay, IDisposable
{
    private static readonly Color SlotChangedGlowColor = Colors.Orange;
    private static readonly Color SlotEmptiedGlowColor = Colors.Red;

    public ItemImageViewModel ItemImageViewModel { get; }

    [ObservableProperty]
    private DecodedItem _item;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SlotNumber))]
    private byte _positionIndex;

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
    public byte RoomId => Item.RoomId;
    public byte Mix => Item.Mix;
    public int Present => Item.Present;

    private bool UsesInventoryEmptyStyle => UsesInventoryEmptyStyleCore(Item);

    public bool IsEmpty => IsItemEmpty(Item);
    public string DisplayName => GetDisplayName(Item);
    public string QuantityText => Quantity.ToString(CultureInfo.InvariantCulture);
    public int SlotNumber => PositionIndex;
    public string DebugInfo => GetDebugInfo(Item);

    public string PickedUpDisplay => GetPickedUpDisplay(Item);
    public bool IsHeldByPlayer => IsItemHeldByPlayer(Item);
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

    partial void OnItemChanged(DecodedItem oldValue, DecodedItem newValue)
    {
        RaiseIfChanged(oldValue.TypeName, newValue.TypeName, nameof(TypeName));

        if (oldValue.Quantity != newValue.Quantity)
        {
            OnPropertyChanged(nameof(Quantity));
            OnPropertyChanged(nameof(QuantityText));
        }

        RaiseIfChanged(oldValue.SlotIndex, newValue.SlotIndex, nameof(SlotIndex));
        RaiseIfChanged(oldValue.PickedUp, newValue.PickedUp, nameof(PickedUp));
        RaiseIfChanged(oldValue.PickedUpByName, newValue.PickedUpByName, nameof(PickedUpByName));
        RaiseIfChanged(oldValue.RoomName, newValue.RoomName, nameof(RoomName));
        RaiseIfChanged(oldValue.RoomId, newValue.RoomId, nameof(RoomId));
        RaiseIfChanged(oldValue.Mix, newValue.Mix, nameof(Mix));
        RaiseIfChanged(oldValue.Present, newValue.Present, nameof(Present));
        RaiseIfChanged(GetPickedUpDisplay(oldValue), GetPickedUpDisplay(newValue), nameof(PickedUpDisplay));
        RaiseIfChanged(IsItemHeldByPlayer(oldValue), IsItemHeldByPlayer(newValue), nameof(IsHeldByPlayer));
        RaiseIfChanged(IsItemEmpty(oldValue), IsItemEmpty(newValue), nameof(IsEmpty));
        RaiseIfChanged(GetDisplayName(oldValue), GetDisplayName(newValue), nameof(DisplayName));
        RaiseIfChanged(GetDebugInfo(oldValue), GetDebugInfo(newValue), nameof(DebugInfo));
    }

    private static Color? DetermineGlowColor(in DecodedItem previousItem, in DecodedItem newItem)
    {
        bool wasEmpty = IsItemEmpty(previousItem);
        bool isEmpty = IsItemEmpty(newItem);

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

    private static bool UsesInventoryEmptyStyleCore(in DecodedItem item) =>
        item is { TypeId: 0, Quantity: 0, PickedUp: 0, Present: 0 };

    private static bool IsItemEmpty(in DecodedItem item) =>
        UsesInventoryEmptyStyleCore(item)
        || string.IsNullOrEmpty(item.TypeName)
        || string.Equals(item.TypeName, "Unknown", StringComparison.Ordinal);

    private static string GetDisplayName(in DecodedItem item) => IsItemEmpty(item) ? "Empty" : item.TypeName;

    private static string GetDebugInfo(in DecodedItem item) =>
        UsesInventoryEmptyStyleCore(item) ? "0x00 | 0" : $"0x{item.Id:X2} | {item.Id}";

    private static string GetPickedUpDisplay(in DecodedItem item) =>
        item.PickedUp > 0 ? $"P{item.PickedUp}" : string.Empty;

    private static bool IsItemHeldByPlayer(in DecodedItem item) => item.PickedUp > 0;

    private void RaiseIfChanged<T>(T oldValue, T newValue, string propertyName)
    {
        if (!EqualityComparer<T>.Default.Equals(oldValue, newValue))
            OnPropertyChanged(propertyName);
    }

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

    public void Dispose() => ItemImageViewModel.Dispose();
}
