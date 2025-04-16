using OutbreakTracker2.Outbreak.Extensions;

namespace OutbreakTracker2.Outbreak.Common;

public class FileTwoPtrs
{
    public const nint DiscStart = 0x023DFD3;

    public const nint BaseLobbySlot = 0x628DA0;
    public const int LobbySlotStructSize = 0x15C;

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

    public const nint LobbyRoomMaxPlayer = 0x5FF77A;
    public const nint LobbyRoomDifficulty = 0x6020CA;
    public const nint LobbyRoomStatus = 0x62DDF0;
    public const nint LobbyRoomScenarioId = 0x62DDF6;
    public const nint LobbyRoomTime = 0x62E768;
    public const nint LobbyRoomCurPlayer = 0x6411E6;

    public const nint BaseLobbyRoomPlayer = 0x630D40;  // Player slot at index 0
    public const int LobbyRoomPlayerStructSize = 0x3A8; // 936-byte structure size 

    public static nint GetLobbyRoomPlayerAddress(int characterID)
    {
        if (!characterID.IsCharacterIdValid())
            throw new InvalidOperationException($"Invalid Character ID: {characterID}");

        return BaseLobbyRoomPlayer + characterID * LobbyRoomPlayerStructSize;
    }

    // Offsets for the lobby room player structure, offseting from BaseLobbyRoomPlayer
    public const nint LobbyRoomPlayerNameIdOffset = 0x0;
    public const nint LobbyRoomPlayerNPCTypeOffset = 0x2;
    public const nint LobbyRoomPlayerEnabledOffset = 0x6;

    const nint Door1HP = 0x477762;//4777A2 Back Alley Door
    const nint Door2HP = 0x477720;//477760 Restaurant Back Door
    const nint Door3HP = 0x477724;//Restaurant Kitchen South
    const nint Door4HP = 0x477726;//Restaurant Kitchen West
    const nint Door5HP = 0x4777E4;//477820 South Area > East Gate
    const nint Door6HP = 0x4777E0;//477864 South Area > North Gate
    const nint Door7HP = 0x477824;//477860 East Area > North Gate
    const nint Door8HP = 0x4779E2;//477A66 Elephant Stage
    const nint Door9HP = 0x4778AE;//477B64 Otherworld East Side
    const nint Door10HP = 0x4777A4;//4778A0 Otherworld West Side
    const nint Door11HP = 0x4778AC;//4779A0 Lounge Right
    const nint Door12HP = 0x4778AA;//4779A2 Lounge Left
    const nint Door13HP = 0x477B26;//477BE0 Memory Room 203
    const nint Door14HP = 0x477724;//4777E0 Lobby > 1F Main Hall
    const nint Door15HP = 0x477824;//477920 Office > 1F East Wing
    const nint Door16HP = 0x4777E4;//477BE0 1F Main Hall > U-shaped Corridor
    const nint Door17HP = 0x4779A0;//4779E0 Morgue
    const nint Door18HP = 0x477A62;//477AA0 Dog House
    const nint Door19HP = 0x477B00;//EOTR Hole HP

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
            10 => Door11HP,
            11 => Door12HP,
            12 => Door13HP,
            13 => Door14HP,
            14 => Door15HP,
            15 => Door16HP,
            16 => Door17HP,
            17 => Door18HP,
            18 => Door19HP,
            _ => -1
        };
    }

    const nint Door1Flag = 0x490234;//490228/490234 Back Alley Door
    const nint Door2Flag = 0x490228;//49021C/490228 Restaurant Back Door
    const nint Door3Flag = 0x49021C;//Restaurant Kitchen South
    const nint Door4Flag = 0x49021C;//Restaurant Kitchen West
    const nint Door5Flag = 0x490240;//490240/49024C South Area > East Gate
    const nint Door6Flag = 0x490240;//490240/490258 South Area > North Gate
    const nint Door7Flag = 0x490258;//49024C/490258 East Area > North Gate
    const nint Door8Flag = 0x4902B8;//4902A0/4902B8 Elephant Stage
    const nint Door9Flag = 0x4902E8;//490264/4902E8 Otherworld East Side
    const nint Door10Flag = 0x490234;//490234/490264 Otherworld West Side
    const nint Door11Flag = 0x490294;//490264/490294 Lounge Right
    const nint Door12Flag = 0x490294;//490264/490294 Lounge Left
    const nint Door13Flag = 0x490300;//490300 4902DC Memory Room 203
    const nint Door14Flag = 0x49021C;//49021C/490240 Lobby > 1F Main Hall
    const nint Door15Flag = 0x49027C;//49024C/49027C Office > 1F East Wing
    const nint Door16Flag = 0x490300;//490240/490300 1F Main Hall > U-shaped Corridor
    const nint Door17Flag = 0x490294;//490294/4902A0 Morgue
    const nint Door18Flag = 0x4902C4;//4902B8/4902C4 Dog House
    const nint Door19Flag = 0x48FFF4;//EOTR Hole HP

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
            10 => Door11Flag,
            11 => Door12Flag,
            12 => Door13Flag,
            13 => Door14Flag,
            14 => Door15Flag,
            15 => Door16Flag,
            16 => Door17Flag,
            17 => Door18Flag,
            18 => Door19Flag,
            _ => -1
        };
    }

    private const nint BasePlayerStart = 0x47BD30; // The start address for the first player
    private const int PlayerStructSize = 0x1100; // Size of 4352 bytes derived from address differences

    public static nint GetPlayerStartAddress(int characterId)
    {
        if (!characterId.IsCharacterIdValid())
            throw new InvalidOperationException($"Invalid character id: {characterId}");

        return BasePlayerStart + characterId * PlayerStructSize;
    }

    public const nint CharacterEnabledOffset  = 0x0; //0
    public const nint CharacterInGameOffset  = 0x1;  //1
    public const nint PositionXOffset = 0x38;        //56
    public const nint PositionYOffset  = 0x40;       //64
    public const nint SizeOffset = 0xB4;             //180
    public const nint RoomIdOffset = 0x1E2;          //482
    public const nint CurHpOffset = 0x540;           //1344
    public const nint MaxHpOffset = 0x542;           //1346
    public const nint CharacterTypeOffset = 0xBB0;   //2992
    public const nint CharacterStatusOffset = 0xBB8; //3000 
    public const nint VirusOffset = 0xBC4;           //3012
    public const nint NameTypeOffset = 0xBC8;        //3016
    public const nint AntiVirusTimeOffset = 0xBCA;   //3018
    public const nint AntiVirusGTimeOffset = 0xBCC;  //3020
    public const nint HerbTimeOffset = 0xBCE;        //3022
    public const nint CritBonusOffset = 0xBD4;       //3028
    public const nint SpeedOffset = 0xBD8;           //3032
    public const nint PowerOffset = 0xBDC;           //3036
    public const nint BleedTimeOffset = 0xC6A;       //3178
    public const nint EquippedItemOffset = 0xC8C;    //3212
    public const nint InventoryOffset = 0xC94;       //3220

    public const nint PickupSpaceStart = 0x397B7C; // 1 item - 60 bytes settings byte on offset ITEM_PTR+37?
    public const nint DeadInventoryStart = 0x491146;
    public const nint VirusMaxStart = 0x728500;
    public const nint ScenarioIDAddr = 0x3137BA;
    public const nint FrameCounter = 0x4912B8;
}
