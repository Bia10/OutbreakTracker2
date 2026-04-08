namespace OutbreakTracker2.Application.Services.Reports.Events;

public sealed record PlayerConditionChangedEvent(
    DateTimeOffset OccurredAt,
    Ulid PlayerId,
    string PlayerName,
    string OldCondition,
    string NewCondition
) : RunEvent(OccurredAt);
