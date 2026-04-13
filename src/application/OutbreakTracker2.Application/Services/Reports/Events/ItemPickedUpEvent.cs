namespace OutbreakTracker2.Application.Services.Reports.Events;

/// <summary>
/// Emitted when a world item transitions from ground (PickedUp == 0) to being held by a player.
/// </summary>
public sealed record ItemPickedUpEvent(
    DateTimeOffset OccurredAt,
    string TypeName,
    byte SlotIndex,
    byte RoomId,
    string PickedUpByName
) : RunEvent(OccurredAt)
{
    public override string Describe(OutbreakTracker2.Outbreak.Enums.Scenario scenario) =>
        string.IsNullOrEmpty(PickedUpByName)
            ? Invariant($"**{TypeName}** (item slot {SlotIndex}) looted from {RoomName(scenario, RoomId)}")
            : Invariant(
                $"**{PickedUpByName}** looted **{TypeName}** (item slot {SlotIndex}) from {RoomName(scenario, RoomId)}"
            );
}
