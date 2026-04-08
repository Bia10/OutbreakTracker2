namespace OutbreakTracker2.Application.Services.Reports.Events;

/// <summary>
/// Emitted when a held item is dropped back to the ground (PickedUp transitions from a player slot to 0).
/// </summary>
public sealed record ItemDroppedEvent(DateTimeOffset OccurredAt, string TypeName, byte RoomId, string PreviousHolder)
    : RunEvent(OccurredAt);
