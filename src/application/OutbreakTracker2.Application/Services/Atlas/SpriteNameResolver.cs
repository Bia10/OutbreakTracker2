using Microsoft.Extensions.Logging;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Enums.Character;
using OutbreakTracker2.Outbreak.Utility;

namespace OutbreakTracker2.Application.Services.Atlas;

public sealed class SpriteNameResolver(ILogger<SpriteNameResolver> logger) : ISpriteNameResolver
{
    private readonly ILogger<SpriteNameResolver> _logger = logger;

    public string GetSpriteNameFromCharacterType(CharacterBaseType characterType)
    {
        string spriteName = EnumUtility.GetEnumString(characterType, CharacterBaseType.Unknown);

        if (!spriteName.StartsWith("bust", StringComparison.OrdinalIgnoreCase))
            spriteName = $"bust{spriteName}";

        _logger.LogDebug(
            "Obtained sprite name '{SpriteName}' for character type '{CharacterType}'",
            spriteName,
            characterType
        );
        return spriteName;
    }

    public string GetSpriteNameFromScenarioName(Scenario scenarioName)
    {
        string spriteName = EnumUtility.GetEnumString(scenarioName, Scenario.Unknown);

        _logger.LogDebug(
            "Obtained sprite name '{SpriteName}' for scenario name '{ScenarioName}'",
            spriteName,
            scenarioName
        );
        return spriteName.ToLowerInvariant();
    }

    public string GetSpriteNameFromItemType(ItemType itemType)
    {
        string spriteName = EnumUtility.GetEnumString(itemType, ItemType.Unknown);

        if (!spriteName.StartsWith("item", StringComparison.OrdinalIgnoreCase))
            spriteName = $"item{spriteName}";

        _logger.LogDebug("Obtained sprite name '{SpriteName}' for item type '{ItemType}'", spriteName, itemType);
        return spriteName;
    }

    public string GetSpriteNameFromItemName(string itemName)
    {
        ArgumentException.ThrowIfNullOrEmpty(itemName);

        string spriteName = itemName;

        // TODO: the format is a bit weird (GameFile/ItemTypeName)
        if (!itemName.StartsWith("File Two/", StringComparison.OrdinalIgnoreCase))
            spriteName = $"File Two/{itemName}";

        _logger.LogDebug("Obtained sprite name '{SpriteName}' for item id '{ItemId}'", spriteName, itemName);
        return spriteName;
    }
}
