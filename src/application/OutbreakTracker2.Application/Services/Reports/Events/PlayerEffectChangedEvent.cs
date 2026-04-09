namespace OutbreakTracker2.Application.Services.Reports.Events;

/// <summary>
/// Emitted when a timed player effect starts or expires.
/// Covers BleedTime, HerbTime, AntiVirusTime, and AntiVirusGTime transitions to/from zero.
/// </summary>
public sealed record PlayerEffectChangedEvent(
    DateTimeOffset OccurredAt,
    Ulid PlayerId,
    string PlayerName,
    string EffectName,
    bool IsApplied
) : RunEvent(OccurredAt);
