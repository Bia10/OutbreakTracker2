using OutbreakTracker2.Application.Services.Reports.Events;

namespace OutbreakTracker2.Application.Services.Reports;

public sealed record RunReportStats
{
    public int TotalEnemySpawns { get; init; }

    public int TotalEnemyKills { get; init; }

    public int TotalDespawns { get; init; }

    public int TotalEnemyDamageEvents { get; init; }

    public int TotalEnemyStatusChanges { get; init; }

    public int TotalEnemyActivations { get; init; }

    public int TotalEnemyRoomTransitions { get; init; }

    public int TotalDamageTaken { get; init; }

    public int TotalPlayerHealthChanges { get; init; }

    public int TotalPlayerHealingEvents { get; init; }

    public int TotalPlayerVirusChanges { get; init; }

    public double PeakVirusPercentage { get; init; }

    public int TotalPlayerJoins { get; init; }

    public int TotalPlayerLeaves { get; init; }

    public int TotalPlayerConditionChanges { get; init; }

    public int TotalPlayerStatusChanges { get; init; }

    public int TotalPlayerEffectChanges { get; init; }

    public int TotalPlayerInventoryChanges { get; init; }

    public int TotalPlayerRoomTransitions { get; init; }

    public int TotalDoorStateChanges { get; init; }

    public int TotalDoorFlagChanges { get; init; }

    public int TotalDoorDamageEvents { get; init; }

    public int TotalItemPickups { get; init; }

    public int TotalItemDrops { get; init; }

    public int TotalItemQuantityChanges { get; init; }

    public int TotalScenarioStatusChanges { get; init; }

    public TimeSpan Duration { get; init; }

    public IReadOnlyDictionary<string, int> EnemyDamageContributedByPlayer { get; init; } =
        new Dictionary<string, int>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, int> KillsContributedByPlayer { get; init; } =
        new Dictionary<string, int>(StringComparer.Ordinal);

    public static RunReportStats From(RunReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        RunReportStatsAccumulator accumulator = new();
        foreach (RunEvent evt in report.Events)
            evt.Accumulate(accumulator);

        return accumulator.Build(report.Duration);
    }
}
