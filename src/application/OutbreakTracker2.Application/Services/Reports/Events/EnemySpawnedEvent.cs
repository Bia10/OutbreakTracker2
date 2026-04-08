namespace OutbreakTracker2.Application.Services.Reports.Events;

public sealed record EnemySpawnedEvent(
    DateTimeOffset OccurredAt,
    Ulid EnemyId,
    string EnemyName,
    short SlotId,
    byte RoomId,
    ushort MaxHp
) : RunEvent(OccurredAt);
