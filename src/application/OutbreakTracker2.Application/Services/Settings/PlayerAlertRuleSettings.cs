namespace OutbreakTracker2.Application.Services.Settings;

public sealed record PlayerAlertRuleSettings
{
    public bool DangerCondition { get; init; } = true;

    public bool GasCondition { get; init; } = true;

    public bool DeadStatus { get; init; } = true;

    public bool ZombieStatus { get; init; } = true;

    public bool DownStatus { get; init; } = true;

    public bool BleedStatus { get; init; } = true;

    public bool HealthZero { get; init; } = true;

    public bool VirusWarningEnabled { get; init; } = true;

    public double VirusWarningThreshold { get; init; } = 50.0;

    public bool VirusCriticalEnabled { get; init; } = true;

    public double VirusCriticalThreshold { get; init; } = 75.0;

    public bool AntiVirusExpired { get; init; } = true;

    public bool AntiVirusGExpired { get; init; } = true;

    public bool BleedStopped { get; init; } = true;

    public bool RoomChange { get; init; } = true;

    public bool Joined { get; init; } = true;

    public bool Left { get; init; } = true;
}
