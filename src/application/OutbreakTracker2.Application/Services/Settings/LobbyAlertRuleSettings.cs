namespace OutbreakTracker2.Application.Services.Settings;

public sealed record LobbyAlertRuleSettings
{
    public bool GameCreated { get; init; } = true;

    public bool NameMatchCreated { get; init; }

    public string NameMatchFilter { get; init; } = string.Empty;

    public bool ScenarioMatchCreated { get; init; }

    public string ScenarioMatchFilter { get; init; } = string.Empty;
}
