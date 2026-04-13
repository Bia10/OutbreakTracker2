namespace OutbreakTracker2.Outbreak.Models;

/// <summary>
/// Fixed-width 4-slot inventory snapshot with value semantics.
/// </summary>
public readonly record struct InventorySnapshot(byte Slot0, byte Slot1, byte Slot2, byte Slot3)
{
    public const int SlotCount = 4;

    public static InventorySnapshot Empty => default;

    public int Length => SlotCount;

    public byte this[int index] =>
        index switch
        {
            0 => Slot0,
            1 => Slot1,
            2 => Slot2,
            3 => Slot3,
            _ => throw new ArgumentOutOfRangeException(nameof(index)),
        };
}
