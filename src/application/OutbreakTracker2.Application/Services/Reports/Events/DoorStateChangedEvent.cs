namespace OutbreakTracker2.Application.Services.Reports.Events;

public sealed record DoorStateChangedEvent(
    DateTimeOffset OccurredAt,
    Ulid DoorId,
    int SlotId,
    string OldStatus,
    string NewStatus
) : RunEvent(OccurredAt);
