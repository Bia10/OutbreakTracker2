namespace OutbreakTracker2.Application.Services.Reports.Events;

/// <summary>
/// Emitted when a held item is dropped back to the ground (PickedUp transitions from a player slot to 0).
/// </summary>
public sealed record ItemDroppedEvent(
    DateTimeOffset OccurredAt,
    string TypeName,
    byte SlotIndex,
    byte RoomId,
    string PreviousHolder
) : RunEvent(OccurredAt)
{
    public override string Describe(OutbreakTracker2.Outbreak.Enums.Scenario scenario) =>
        string.IsNullOrEmpty(PreviousHolder)
            ? Invariant($"**{TypeName}** (item slot {SlotIndex}) dropped at {RoomName(scenario, RoomId)}")
            : Invariant(
                $"**{PreviousHolder}** dropped **{TypeName}** (item slot {SlotIndex}) at {RoomName(scenario, RoomId)}"
            );

    internal override void Accumulate(IRunEventStatsAccumulator accumulator) => accumulator.Accumulate(this);
}
