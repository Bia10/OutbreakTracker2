namespace OutbreakTracker2.Application.Services.Reports.Events;

public sealed record ItemQuantityChangedEvent(
    DateTimeOffset OccurredAt,
    string TypeName,
    byte SlotIndex,
    byte RoomId,
    short OldQuantity,
    short NewQuantity
) : RunEvent(OccurredAt);
