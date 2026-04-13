namespace OutbreakTracker2.Application.Services.Settings;

public sealed record DisplaySettings
{
    public bool ShowGameplayUiDuringTransitions { get; init; }

    public EntitiesDockSettings EntitiesDock { get; init; } = new();
    public ScenarioItemsDockSettings ScenarioItemsDock { get; init; } = new();
}
