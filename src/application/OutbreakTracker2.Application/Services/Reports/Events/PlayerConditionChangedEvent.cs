using OutbreakTracker2.Outbreak.Enums;

namespace OutbreakTracker2.Application.Services.Reports.Events;

public sealed record PlayerConditionChangedEvent(
    DateTimeOffset OccurredAt,
    Ulid PlayerId,
    string PlayerName,
    string OldCondition,
    string NewCondition
) : RunEvent(OccurredAt)
{
    public override string Describe(Scenario scenario) =>
        Invariant($"Player **{PlayerName}** condition: {OldCondition} → **{NewCondition}**");
}
