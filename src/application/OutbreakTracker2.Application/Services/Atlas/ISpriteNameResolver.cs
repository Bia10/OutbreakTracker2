using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Enums.Character;

namespace OutbreakTracker2.Application.Services.Atlas;

public interface ISpriteNameResolver
{
    public string GetSpriteNameFromCharacterType(CharacterBaseType characterType);

    public string GetSpriteNameFromScenarioName(Scenario scenarioName);

    public string GetSpriteNameFromItemType(ItemType itemType);

    public string GetSpriteNameFromItemName(string itemName);
}
