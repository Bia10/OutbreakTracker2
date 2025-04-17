using OutbreakTracker2.Outbreak.Extensions;

namespace OutbreakTracker2.Outbreak.Common;

public class FileOnePtrs
{
    // Actually a bit shady maybe we could get that from the PCSX2 text memory
    public const nint DiscStart = 0x02321B3;

    public const nint BaseLobbySlot = 0x629600; // The slot at index 0
    public const int LobbySlotStructSize = 0x15C; // Same 348-byte structure size calculated from address differences

    public static nint GetLobbyAddress(int slotIndex)
    {
        if (!slotIndex.IsSlotIndexValid())
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

    public const nint LobbyRoomMaxPlayer = 0x5FFFDA;
    public const nint LobbyRoomDifficulty = 0x60292A;
    public const nint LobbyRoomStatus = 0x62E230;
    public const nint LobbyRoomScenarioId = 0x62E236;
    public const nint LobbyRoomTime = 0x62EB80;
    public const nint LobbyRoomCurPlayer = 0x6547AA;

    public const nint BaseLobbyRoomPlayer = 0x630E54; // Player slot in lobby room at index 0
    public const int LobbyRoomPlayerStructSize = 0x3A8; // 936-byte structure size calculated from address differences

    public static nint GetLobbyRoomPlayerAddress(int characterId)
    {
        if (!characterId.IsCharacterIdValid())
            throw new InvalidOperationException($"Invalid character id: {characterId}");

        return BaseLobbyRoomPlayer + characterId * LobbyRoomPlayerStructSize;
    }

    // Offsets for the lobby room player structure, offseting from BaseLobbyRoomPlayer
    public const nint LobbyRoomPlayerEnabledOffset = 0x0;
    public const nint LobbyRoomPlayerNameIdOffset = 0xE4;
    public const nint LobbyRoomPlayerNPCTypeOffset = 0xE6;

    private const nint Door1HP = 0x472FC0;//472FC0/472FF0
    private const nint Door2HP = 0x473086;//473086
    private const nint Door3HP = 0x4731C0;//4731C0/473220
    private const nint Door4HP = 0x473232;//473232/473260
    private const nint Door5HP = 0x473504;//473504/4735F0
    private const nint Door6HP = 0x473652;//room 102
                             // +0xC0 192 bytes
    private const nint Door7HP = 0x473712;//room 201
                             // +0xC0 192 bytes        
    private const nint Door8HP = 0x4737D2;//room 202
    private const nint Door9HP = 0x4739B6;//4739B6/473A70 dd t-shaped
    private const nint Door10HP = 0x473A40;//473A40/473A72 store room

    public static nint GetDoorHealthAddress(int doorID)
    {
        return doorID switch
        {
            0 => Door1HP,
            1 => Door2HP,
            2 => Door3HP,
            3 => Door4HP,
            4 => Door5HP,
            5 => Door6HP,
            6 => Door7HP,
            7 => Door8HP,
            8 => Door9HP,
            9 => Door10HP,
            _ => -1
        };
    }

    private const nint Door1Flag = 0x48AFBC;//48AFBC
    private const nint Door2Flag = 0x48AFEC;//48AFEC
    private const nint Door3Flag = 0x48AD90;//
    private const nint Door4Flag = 0x48B058;//48B058
    private const nint Door5Flag = 0x48B10C;//48B10C/48B148
    private const nint Door6Flag = 0x48B160;//room 102
                               // +0x30 (48 bytes)
    private const nint Door7Flag = 0x48B190;//room 201
                               // +0x30 (48 bytes)
    private const nint Door8Flag = 0x48B1C0;//room 202
    private const nint Door9Flag = 0x48B238;//48B238/48B268 dd t-shaped
    private const nint Door10Flag = 0x48B25C;//48B25C/48B268 store room

    public static nint GetDoorFlagAddress(int doorID)
    {
        return doorID switch
        {
            0 => Door1Flag,
            1 => Door2Flag,
            2 => Door3Flag,
            3 => Door4Flag,
            4 => Door5Flag,
            5 => Door6Flag,
            6 => Door7Flag,
            7 => Door8Flag,
            8 => Door9Flag,
            9 => Door10Flag,
            _ => -1
        };
    }

    private const nint BasePlayerStart = 0x476DD0; // The start address for the first player
    private const int PlayerStructSize = 0x10E0; // Size of 4320 bytes derived from address differences

    public static nint GetPlayerStartAddress(int characterId)
    {
        if (!characterId.IsCharacterIdValid())
            throw new InvalidOperationException($"Invalid character id: {characterId}");

        return BasePlayerStart + characterId * PlayerStructSize;
    }

    public const nint CharacterEnabledOffset = 0x0; // 0 +1 byte(s) to next offset
    public const nint CharacterInGameOffset = 0x1;  // 1 +55 byte(s) to next offset
    public const nint PositionXOffset = 0x38;       // 56 +8 byte(s) to next offset
    public const nint PositionYOffset = 0x40;       // 64 +116 byte(s) to next offset
    public const nint SizeOffset = 0xB4;            // 180 +302 byte(s) to next offset
    public const nint RoomIdOffset = 0x1E2;         // 482 +866 byte(s) to next offset
    public const nint CurHpOffset = 0x544;          // 1348 +2 byte(s) to next offset
    public const nint MaxHpOffset = 0x546;          // 1350 +1618 byte(s) to next offset
    public const nint CharacterTypeOffset = 0xB98;  // 2968 +8 byte(s) to next offset
    public const nint CharacterStatusOffset = 0xBA0;// 2976 +12 byte(s) to next offset
    public const nint VirusOffset = 0xBAC;          // 2988 +4 byte(s) to next offset
    public const nint CritBonusOffset = 0xBB0;      // 2992 +4 byte(s) to next offset
    public const nint NameTypeOffset = 0xBB4;       // 2996 +2 byte(s) to next offset
    public const nint AntiVirusTimeOffset = 0xBB6;  // 2998 +2 byte(s) to next offset
    public const nint AntiVirusGTimeOffset = 0xBB8; // 3000 +2 byte(s) to next offset
    public const nint HerbTimeOffset = 0xBBA;       // 3002 +6 byte(s) to next offset
    public const nint SpeedOffset = 0xBC0;          // 3008 +4 byte(s) to next offset
    public const nint PowerOffset = 0xBC4;          // 3012 +150 byte(s) to next offset
    public const nint BleedTimeOffset = 0xC5A;      // 3162 +34 byte(s) to next offset
    public const nint EquippedItemOffset = 0xC7C;   // 3196 +8 byte(s) to next offset
    public const nint InventoryOffset = 0xC84;      // 3204

    public const nint DeadInventoryStart = 0x48BDE2;
    public const nint VirusMaxStart = 0x6E6C70;

    public const nint IngameScenarioId = 0x3065AA;
    public const nint IngameFrameCounter = 0x48BF78;
    public const nint Pass1 = 0x48AC13;
    public const nint Pass2 = 0x48AC17;
    public const nint Pass3 = 0x48AC14;
    public const nint Pass4 = 0x48AC1A; //48AC1A 48ADCE
    public const nint Pass5 = 0x48AC1B; //48ADCF  on=48ADF3
    public const nint Pass6 = 0x48AC15; //4927=7500 4032=7480 40 4284=0200
    public const nint Difficulty = 0x48C01A;
    public const nint IsScenarioCleared = 0x48BF60; //
    public const nint ItemRandom = 0x23BB20; //23BB28 23C055 23BD20 426AA9 23BBA0 23BD24
    public const nint ItemRandom2 = 0x23BBA1;
    public const nint PuzzleRandom = 0x23BBA0; // puzzle set
    public const nint IngamePlayerNumber =  0x23BE14;//23C004
}
