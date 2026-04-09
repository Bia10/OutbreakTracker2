namespace OutbreakTracker2.Application.Services.Reports.Events;

/// <summary>
/// Emitted when an item slot inside a player's inventory changes.
/// Covers Main (4 slots), Special (4 slots), Dead (4 slots), and SpecialDead (4 slots) inventories.
/// </summary>
public sealed record PlayerInventoryChangedEvent(
    DateTimeOffset OccurredAt,
    Ulid PlayerId,
    string PlayerName,
    InventoryKind Kind,
    int SlotIndex,
    string OldItemName,
    string NewItemName
) : RunEvent(OccurredAt);
