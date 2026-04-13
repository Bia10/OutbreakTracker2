namespace OutbreakTracker2.Application.Services.Reports.Events;

public sealed record ItemQuantityChangedEvent(
    DateTimeOffset OccurredAt,
    string TypeName,
    byte SlotIndex,
    byte RoomId,
    short OldQuantity,
    short NewQuantity
) : RunEvent(OccurredAt)
{
    public override string Describe(OutbreakTracker2.Outbreak.Enums.Scenario scenario) =>
        Invariant(
            $"**{TypeName}** (slot {SlotIndex}) quantity: {OldQuantity} → **{NewQuantity}** ({RoomName(scenario, RoomId)})"
        );

    internal override void Accumulate(IRunEventStatsAccumulator accumulator) => accumulator.Accumulate(this);
}
