namespace OutbreakTracker2.Application.Services.Reports.Events;

public sealed record DoorDamagedEvent(DateTimeOffset OccurredAt, Ulid DoorId, ushort OldHp, ushort NewHp)
    : RunEvent(OccurredAt)
{
    public ushort Damage => (ushort)(OldHp - NewHp);
}
