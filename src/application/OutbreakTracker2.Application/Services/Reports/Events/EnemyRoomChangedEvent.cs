namespace OutbreakTracker2.Application.Services.Reports.Events;

public sealed record EnemyRoomChangedEvent(
    DateTimeOffset OccurredAt,
    Ulid EnemyId,
    string EnemyName,
    short SlotId,
    byte OldRoomId,
    byte NewRoomId
) : RunEvent(OccurredAt)
{
    public override string Describe(OutbreakTracker2.Outbreak.Enums.Scenario scenario) =>
        Invariant(
            $"Enemy **{EnemyName}** (Slot {SlotId}) moved room: {RoomName(scenario, OldRoomId)} → **{RoomName(scenario, NewRoomId)}**"
        );

    internal override void Accumulate(IRunEventStatsAccumulator accumulator) => accumulator.Accumulate(this);
}
