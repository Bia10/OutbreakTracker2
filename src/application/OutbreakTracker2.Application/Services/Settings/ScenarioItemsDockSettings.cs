namespace OutbreakTracker2.Application.Services.Settings;

public sealed record ScenarioItemsDockSettings
{
    public bool OnlyShowCurrentPlayerRoom { get; init; } = true;

    public bool ProjectAllOntoMap { get; init; }
}
