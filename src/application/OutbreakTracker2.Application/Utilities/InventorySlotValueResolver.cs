using System.Globalization;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Utilities;

internal static class InventorySlotValueResolver
{
    public static ResolvedInventorySlotValue Resolve(byte itemId, DecodedItem[] scenarioItems)
    {
        ArgumentNullException.ThrowIfNull(scenarioItems);

        if (itemId == 0x00)
            return new(itemId, "Empty", "0", FormatRawValue(itemId));

        foreach (DecodedItem item in scenarioItems)
        {
            if (!IsValidItem(item) || !item.Id.Equals(itemId))
                continue;

            return new(
                itemId,
                item.TypeName,
                item.Quantity.ToString(CultureInfo.InvariantCulture),
                FormatRawValue(itemId)
            );
        }

        return new(itemId, "Unknown", "0", FormatRawValue(itemId));
    }

    public static string FormatRawValue(byte itemId) => $"0x{itemId:X2} | {itemId}";

    private static bool IsValidItem(DecodedItem item) => item is not { SlotIndex: 0, PickedUp: 0 };
}
