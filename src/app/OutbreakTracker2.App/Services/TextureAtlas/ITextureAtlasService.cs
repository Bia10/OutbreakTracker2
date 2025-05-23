using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Enums.Character;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OutbreakTracker2.App.Services.TextureAtlas;

public interface ITextureAtlasService
{
    ITextureAtlas GetAtlas(string name);

    IReadOnlyDictionary<string, ITextureAtlas> GetAllAtlases();

    void LoadAtlases();

    Task LoadAtlasesAsync();

    string GetSpriteNameFromCharacterType(CharacterBaseType characterType);

    string GetSpriteNameFromScenarioName(Scenario scenarioName);
}