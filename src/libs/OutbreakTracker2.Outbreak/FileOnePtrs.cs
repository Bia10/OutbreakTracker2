namespace OutbreakTracker2.Outbreak;

public class FileOnePtrs
{
    // Actually a bit shady maybe we could get that from the PCSX2 text memory
    public const nint DiscStart = 0x02321B3;

    public const nint BaseLobbySlot = 0x629600; // The slot at index 0
    public const int LobbySlotStructSize = 0x15C; // Same 348-byte structure size calculated from address differences

    public static nint GetLobbyAddress(int slotIndex)
    {
        if (slotIndex is < 0 or >= Constants.MaxLobbySlots)
            throw new InvalidOperationException($"Invalid Slot Index: {slotIndex}");

        return BaseLobbySlot + slotIndex * LobbySlotStructSize;
    }

    public const nint LobbySlotPlayer = 0x2;
    public const nint LobbySlotMaxPlayer = 0x4;
    public const nint LobbySlotStatus = 0xE;
    public const nint LobbySlotPass = 0xF;
    public const nint LobbySlotScenarioID = 0x14;
    public const nint LobbySlotVersion = 0x16;
    public const nint LobbySlotTitle = 0x18;
}
