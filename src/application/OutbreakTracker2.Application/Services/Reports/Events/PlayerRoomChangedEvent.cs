namespace OutbreakTracker2.Application.Services.Reports.Events;

public sealed record PlayerRoomChangedEvent(
    DateTimeOffset OccurredAt,
    Ulid PlayerId,
    string PlayerName,
    short OldRoomId,
    short NewRoomId
) : RunEvent(OccurredAt)
{
    public override string Describe(OutbreakTracker2.Outbreak.Enums.Scenario scenario) =>
        Invariant(
            $"Player **{PlayerName}** moved room: {RoomName(scenario, OldRoomId)} → **{RoomName(scenario, NewRoomId)}**"
        );
}
