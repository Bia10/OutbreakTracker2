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
            $"**{TypeName}** at scenario slot {SlotIndex} at {RoomName(scenario, RoomId)} changed quantity: {OldQuantity} → **{NewQuantity}**"
        );

    internal override void Accumulate(IRunEventStatsAccumulator accumulator) => accumulator.Accumulate(this);
}
