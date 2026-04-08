namespace OutbreakTracker2.Outbreak.Common;

/// <summary>
/// Offsets within a single lobby-slot record, shared between File 1 and File 2.
/// Both game files use the same 348-byte slot structure; only the base addresses differ.
/// </summary>
public static class LobbySlotStructOffsets
{
    public const int StructSize = 0x15C; // 348-byte structure

    public const nint Index = 0x0;
    public const nint Player = 0x2;
    public const nint MaxPlayer = 0x4;
    public const nint Status = 0xE;
    public const nint Pass = 0xF;
    public const nint ScenarioId = 0x14;
    public const nint Version = 0x16;
    public const nint Title = 0x18;
}
