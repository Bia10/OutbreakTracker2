namespace OutbreakTracker2.Application.Services.Reports.Events;

/// <summary>
/// Emitted when a world item transitions from ground (PickedUp == 0) to being held by a player.
/// </summary>
public sealed record ItemPickedUpEvent(DateTimeOffset OccurredAt, string TypeName, byte RoomId, string PickedUpByName)
    : RunEvent(OccurredAt);
