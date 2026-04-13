using System.Globalization;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Views.Common.Item;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Utility;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.Inventory;

public sealed partial class ItemSlotViewModel(
    ILogger<ItemSlotViewModel> logger,
    IItemImageViewModelFactory itemImageViewModelFactory
) : ObservableObject, IItemSlotDisplay, IDisposable
{
    private readonly ILogger<ItemSlotViewModel> _logger = logger;

    public ItemImageViewModel ItemImageViewModel { get; } = itemImageViewModelFactory.Create();

    [ObservableProperty]
    private int _slotNumber;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private string _itemName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(QuantityText))]
    private string _itemCount = string.Empty;

    [ObservableProperty]
    private string _debugInfo = string.Empty;

    public string DisplayName => string.IsNullOrEmpty(ItemName) ? "Empty" : ItemName;
    public string QuantityText => ItemCount;
    public bool IsEmpty => string.IsNullOrEmpty(ItemName) || ItemName.Equals("Empty", StringComparison.Ordinal);

    /// <summary>Raised whenever an inventory slot event is detected, carrying the glow color.</summary>
    public event EventHandler<GlowEventArgs>? GlowTriggered;

    private const string EmptySlotName = "Empty";
    private const string EmptySlotCount = "0";

    private string _previousItemName = EmptySlotName;
    private string _previousItemCount = EmptySlotCount;
    private bool _isFirstUpdate = true;

    public void UpdateDisplay(string name, string count, string debug, GameFile gameFile)
    {
        if (!_isFirstUpdate)
        {
            Color? glowColor = DetermineGlowColor(name, count);
            if (glowColor.HasValue)
                GlowTriggered?.Invoke(this, new GlowEventArgs(glowColor.Value));
        }

        bool nameChanged = !name.Equals(_previousItemName, StringComparison.Ordinal);
        bool countChanged = !count.Equals(_previousItemCount, StringComparison.Ordinal);

        _isFirstUpdate = false;

        if (!nameChanged && !countChanged)
            return;

        _previousItemName = name;
        _previousItemCount = count;

        ItemName = name;
        ItemCount = count;
        DebugInfo = debug;

        if (name.Equals(EmptySlotName, StringComparison.Ordinal))
            _ = ItemImageViewModel.ClearImageAsync();
        else
        {
            string currentFile = EnumUtility.GetEnumString(gameFile, GameFile.Unknown);
            string itemSpriteLookupName = currentFile + "/" + name;

            _ = ItemImageViewModel.UpdateImageAsync(itemSpriteLookupName);
        }

        _logger.LogTrace("ItemSlotViewModel updated for item: {ItemName}", name);
    }

    public void ClearDisplay()
    {
        ItemName = string.Empty;
        DebugInfo = string.Empty;

        _logger.LogInformation("ItemSlotViewModel cleared");
    }

    private Color? DetermineGlowColor(string name, string count)
    {
        bool wasEmpty = _previousItemName.Equals(EmptySlotName, StringComparison.Ordinal);
        bool isEmpty = name.Equals(EmptySlotName, StringComparison.Ordinal);

        // Slot free -> occupied: item gained (green)
        if (wasEmpty && !isEmpty)
            return Colors.LimeGreen;

        // Slot occupied -> emptied: item lost (red)
        if (!wasEmpty && isEmpty)
            return Colors.Red;

        // Item type changed to a different non-empty item (treat as gained)
        if (!wasEmpty && !isEmpty && !name.Equals(_previousItemName, StringComparison.Ordinal))
            return Colors.LimeGreen;

        if (
            !isEmpty
            && int.TryParse(count, CultureInfo.InvariantCulture, out int currentCount)
            && int.TryParse(_previousItemCount, CultureInfo.InvariantCulture, out int previousCount)
        )
        {
            // Item quantity++: item gained quantity (blue)
            if (currentCount > previousCount)
                return Colors.DodgerBlue;

            // Item quantity--: item lost quantity (orange)
            if (currentCount < previousCount)
                return Colors.Orange;
        }

        return null;
    }

    public void Dispose() => ItemImageViewModel.Dispose();
}
