namespace OutbreakTracker2.Application.Services.Atlas;

public sealed record TextureAtlasDefinition
{
    public string Name { get; init; } = string.Empty;

    public string JsonPath { get; init; } = string.Empty;

    public string ImagePath { get; init; } = string.Empty;
}
