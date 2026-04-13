namespace OutbreakTracker2.Application.Services.Data;

public sealed record DataManagerOptions
{
    public const string SectionName = "DataManager";

    public int FastUpdateIntervalMs { get; init; } = 250;

    public int SlowUpdateIntervalMs { get; init; } = 500;
}
