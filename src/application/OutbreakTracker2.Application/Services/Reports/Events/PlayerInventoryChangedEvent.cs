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
    byte OldItemId,
    string OldItemName,
    byte NewItemId,
    string NewItemName
) : RunEvent(OccurredAt)
{
    public override string Describe(OutbreakTracker2.Outbreak.Enums.Scenario scenario) =>
        Invariant(
            $"Player **{PlayerName}** {Kind} slot {SlotIndex} changed from {FormatItemValue(OldItemName, OldItemId)} to **{FormatItemValue(NewItemName, NewItemId)}**."
        );

    internal override void Accumulate(IRunEventStatsAccumulator accumulator) => accumulator.Accumulate(this);

    private static string FormatItemValue(string itemName, byte itemId) => $"{itemName} ({FormatRawValue(itemId)})";

    private static string FormatRawValue(byte itemId) => $"0x{itemId:X2} | {itemId}";
}
