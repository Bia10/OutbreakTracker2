namespace OutbreakTracker2.Application.Services.Settings;

public sealed record EnemyAlertRuleSettings
{
    public bool Spawned { get; init; } = true;

    public bool Killed { get; init; } = true;

    public bool Despawned { get; init; } = true;

    public bool RoomChange { get; init; } = true;
}
