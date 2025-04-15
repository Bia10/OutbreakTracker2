using OutbreakTracker2.Outbreak.Common;

namespace OutbreakTracker2.Outbreak.Extensions;

public static class IntExtensions
{
    public static bool IsSlotIndexValid(this int slotIndex)
        => slotIndex is >= 0 and < Constants.MaxLobbySlots;

    public static bool IsCharacterIdValid(this int characterId)
        => characterId is >= 0 and < Constants.MaxMainCharacters;
}