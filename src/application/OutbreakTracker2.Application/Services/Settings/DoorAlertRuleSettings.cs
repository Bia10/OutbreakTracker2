namespace OutbreakTracker2.Application.Services.Settings;

public sealed record DoorAlertRuleSettings
{
    public bool FlagChanged { get; init; } = true;

    public bool Destroyed { get; init; } = true;

    public bool StatusChanged { get; init; } = true;
}
