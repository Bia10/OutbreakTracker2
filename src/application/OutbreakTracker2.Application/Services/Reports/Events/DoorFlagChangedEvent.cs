namespace OutbreakTracker2.Application.Services.Reports.Events;

public sealed record DoorFlagChangedEvent(
    DateTimeOffset OccurredAt,
    Ulid DoorId,
    int SlotId,
    ushort OldFlag,
    ushort NewFlag
) : RunEvent(OccurredAt);
