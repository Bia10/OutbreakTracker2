namespace OutbreakTracker2.Application.Services.Settings;

public sealed record DisplaySettings
{
    public EntitiesDockSettings EntitiesDock { get; init; } = new();
}
