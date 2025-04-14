using OutbreakTracker2.Outbreak.Extensions;

namespace OutbreakTracker2.Outbreak.Common;

public class FileTwoPtrs
{
    public const nint DiscStart = 0x023DFD3;

    public const nint BaseLobbySlot = 0x628DA0;
    public const int LobbySlotStructSize = 0x15C;

    public static nint GetLobbyAddress(int slotIndex)
    {
        if (slotIndex.IsSlotIndexValid())
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

    public const nint LobbyRoomMaxPlayer = 0x5FF77A;
    public const nint LobbyRoomDifficulty = 0x6020CA;
    public const nint LobbyRoomStatus = 0x62DDF0;
    public const nint LobbyRoomScenarioId = 0x62DDF6;
    public const nint LobbyRoomTime = 0x62E768;
    public const nint LobbyRoomCurPlayer = 0x6411E6;
}
