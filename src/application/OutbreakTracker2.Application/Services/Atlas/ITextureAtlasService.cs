namespace OutbreakTracker2.Application.Services.Atlas;

public interface ITextureAtlasService
{
    public ITextureAtlas GetAtlas(string name);

    public IReadOnlyDictionary<string, ITextureAtlas> GetAllAtlases();

    public Task LoadAtlasesAsync();
}
