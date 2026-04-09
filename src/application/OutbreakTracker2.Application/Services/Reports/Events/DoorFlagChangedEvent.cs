namespace OutbreakTracker2.Application.Services.Reports.Events;

public sealed record DoorFlagChangedEvent(DateTimeOffset OccurredAt, Ulid DoorId, ushort OldFlag, ushort NewFlag)
    : RunEvent(OccurredAt);
