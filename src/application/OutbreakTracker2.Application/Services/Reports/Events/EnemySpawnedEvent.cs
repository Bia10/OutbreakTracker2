namespace OutbreakTracker2.Application.Services.Reports.Events;

public sealed record EnemySpawnedEvent(
    DateTimeOffset OccurredAt,
    Ulid EnemyId,
    string EnemyName,
    short SlotId,
    byte RoomId,
    ushort MaxHp
) : RunEvent(OccurredAt)
{
    public override string Describe(OutbreakTracker2.Outbreak.Enums.Scenario scenario) =>
        Invariant($"Enemy **{EnemyName}** spawned ({RoomName(scenario, RoomId)}, Slot {SlotId}, HP: {MaxHp})");

    internal override void Accumulate(IRunEventStatsAccumulator accumulator) => accumulator.Accumulate(this);
}
