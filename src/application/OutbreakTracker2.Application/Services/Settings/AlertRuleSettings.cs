namespace OutbreakTracker2.Application.Services.Settings;

public sealed record AlertRuleSettings
{
    public PlayerAlertRuleSettings Players { get; init; } = new();

    public EnemyAlertRuleSettings Enemies { get; init; } = new();

    public DoorAlertRuleSettings Doors { get; init; } = new();

    public LobbyAlertRuleSettings Lobby { get; init; } = new();
}
