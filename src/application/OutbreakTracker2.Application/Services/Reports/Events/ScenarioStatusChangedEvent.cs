using OutbreakTracker2.Outbreak.Enums;

namespace OutbreakTracker2.Application.Services.Reports.Events;

public sealed record ScenarioStatusChangedEvent(
    DateTimeOffset OccurredAt,
    ScenarioStatus OldStatus,
    ScenarioStatus NewStatus
) : RunEvent(OccurredAt)
{
    public override string Describe(Scenario scenario) => Invariant($"Scenario status: {OldStatus} → **{NewStatus}**");
}
