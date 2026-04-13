using OutbreakTracker2.Outbreak.Enums.Character;

namespace OutbreakTracker2.Outbreak.Utility;

public static class CharacterInventoryUtility
{
    public static bool HasSpecialInventory(string characterType) =>
        EnumUtility.TryParseByValueOrMember(characterType, out CharacterBaseType parsedCharacterType)
        && HasSpecialInventory(parsedCharacterType);

    public static bool HasSpecialInventory(CharacterBaseType characterType) =>
        characterType is CharacterBaseType.Yoko or CharacterBaseType.Cindy or CharacterBaseType.David;
}
