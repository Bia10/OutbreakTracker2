namespace OutbreakTracker2.Application.Services.Reports.Events;

public sealed record PlayerRoomChangedEvent(
    DateTimeOffset OccurredAt,
    Ulid PlayerId,
    string PlayerName,
    short OldRoomId,
    short NewRoomId
) : RunEvent(OccurredAt);
