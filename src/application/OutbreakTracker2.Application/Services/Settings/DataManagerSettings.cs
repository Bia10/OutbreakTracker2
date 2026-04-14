namespace OutbreakTracker2.Application.Services.Settings;

public sealed record DataManagerSettings
{
    public int FastUpdateIntervalMs { get; init; } = 250;

    public int SlowUpdateIntervalMs { get; init; } = 500;
}
