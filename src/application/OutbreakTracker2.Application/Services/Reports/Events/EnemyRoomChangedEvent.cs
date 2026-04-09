namespace OutbreakTracker2.Application.Services.Reports.Events;

public sealed record EnemyRoomChangedEvent(
    DateTimeOffset OccurredAt,
    Ulid EnemyId,
    string EnemyName,
    short SlotId,
    byte OldRoomId,
    byte NewRoomId
) : RunEvent(OccurredAt);
