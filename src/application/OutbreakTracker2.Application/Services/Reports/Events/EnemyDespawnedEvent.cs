namespace OutbreakTracker2.Application.Services.Reports.Events;

/// <summary>
/// Emitted when an enemy is removed from the collection while still alive (scripted despawn,
/// room transition, or scenario event) rather than being killed by players.
/// </summary>
public sealed record EnemyDespawnedEvent(
    DateTimeOffset OccurredAt,
    Ulid EnemyId,
    string EnemyName,
    short SlotId,
    byte RoomId,
    ushort RemainingHp,
    ushort MaxHp
) : RunEvent(OccurredAt)
{
    public override string Describe(OutbreakTracker2.Outbreak.Enums.Scenario scenario) =>
        Invariant(
            $"Enemy **{EnemyName}** despawned ({RoomName(scenario, RoomId)}, Slot {SlotId}, HP remaining: {RemainingHp}/{MaxHp})"
        );

    internal override void Accumulate(IRunEventStatsAccumulator accumulator) => accumulator.Accumulate(this);
}
