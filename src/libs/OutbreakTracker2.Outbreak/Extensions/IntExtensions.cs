using OutbreakTracker2.Outbreak.Common;

namespace OutbreakTracker2.Outbreak.Extensions;

public static class IntExtensions
{
    public static bool IsSlotIndexValid(this int slotIndex)
        => slotIndex is >= 0 and < Constants.MaxLobbySlots;
}