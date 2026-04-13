namespace OutbreakTracker2.Application.Services.Reports.Events;

public sealed record DoorFlagChangedEvent(
    DateTimeOffset OccurredAt,
    Ulid DoorId,
    int SlotId,
    ushort OldFlag,
    ushort NewFlag
) : RunEvent(OccurredAt)
{
    public override string Describe(OutbreakTracker2.Outbreak.Enums.Scenario scenario) =>
        Invariant($"Door #{SlotId} flag changed: 0x{OldFlag:X4} → **0x{NewFlag:X4}**");
}
