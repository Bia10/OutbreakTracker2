using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Enums.Character;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OutbreakTracker2.Application.Services.Atlas;

public interface ITextureAtlasService
{
    public ITextureAtlas GetAtlas(string name);

    public IReadOnlyDictionary<string, ITextureAtlas> GetAllAtlases();

    public void LoadAtlases();

    public Task LoadAtlasesAsync();

    public string GetSpriteNameFromCharacterType(CharacterBaseType characterType);

    public string GetSpriteNameFromScenarioName(Scenario scenarioName);

    public string GetSpriteNameFromItemType(ItemType itemType);

    public string GetSpriteNameFromItemName(string itemName);
}