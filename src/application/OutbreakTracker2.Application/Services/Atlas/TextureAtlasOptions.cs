namespace OutbreakTracker2.Application.Services.Atlas;

public sealed record TextureAtlasOptions
{
    public const string SectionName = "TextureAtlases";

    public IReadOnlyList<TextureAtlasDefinition> Atlases { get; init; } = [];
}
