using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.PCSX2;

namespace OutbreakTracker2.Outbreak.Readers;

public class InGamePlayerReader : ReaderBase
{
    public InGamePlayerReader(GameClient gameClient, EEmemMemory eememMemory) : base(gameClient, eememMemory)
    {
    }

    public bool GetIsEnabled(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<bool>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.CharacterEnabledOffset, nameof(GetIsEnabled)),
        GameFile.FileTwo => ReadValue<bool>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.CharacterEnabledOffset, nameof(GetIsEnabled)),
        _ => false
    };

    public bool GetIsInGame(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<bool>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.CharacterInGameOffset, nameof(GetIsInGame)),
        GameFile.FileTwo => ReadValue<bool>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.CharacterInGameOffset, nameof(GetIsInGame)),
        _ => false
    };

    public  short GetRoomId(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<short>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.RoomIdOffset, nameof(GetRoomId)),
        GameFile.FileTwo => ReadValue<short>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.RoomIdOffset, nameof(GetRoomId)),
        _ => 0xFF
    };

    public  short GetCurHealth(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<short>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.CurHpOffset, nameof(GetCurHealth)),
        GameFile.FileTwo => ReadValue<short>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.CurHpOffset, nameof(GetCurHealth)),
        _ => 0xFF
    };

    public  short GetMaxHealth(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<short>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.MaxHpOffset, nameof(GetMaxHealth)),
        GameFile.FileTwo => ReadValue<short>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.MaxHpOffset, nameof(GetMaxHealth)),
        _ => 0xFF
    };

    public  byte GetType(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<byte>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.CharacterTypeOffset, nameof(GetType)),
        GameFile.FileTwo => ReadValue<byte>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.CharacterTypeOffset, nameof(GetType)),
        _ => 0xFF
    };

    public  byte GetInventory(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<byte>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.InventoryOffset, nameof(GetInventory)),
        GameFile.FileTwo => ReadValue<byte>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.InventoryOffset, nameof(GetInventory)),
        _ => 0xFF
    };

    public byte GetSpecialItem(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<byte>((FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.InventoryOffset + 4), nameof(GetSpecialItem)),
        GameFile.FileTwo => ReadValue<byte>((FileOnePtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.InventoryOffset + 4), nameof(GetSpecialItem)),
        _ => 0xFF
    };

    public byte GetSpecialInventory(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<byte>((FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.InventoryOffset + 5), nameof(GetSpecialItem)),
        GameFile.FileTwo => ReadValue<byte>((FileOnePtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.InventoryOffset + 5), nameof(GetSpecialItem)),
        _ => 0xFF
    };

    public byte GetDeadInventory(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<byte>(FileOnePtrs.DeadInventoryStart + 8 * characterId, nameof(GetDeadInventory)),
        GameFile.FileTwo => ReadValue<byte>(FileTwoPtrs.DeadInventoryStart + 8 * characterId, nameof(GetDeadInventory)),
        _ => 0xFF
    };

    public byte GetSpecialDeadInventory(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<byte>(FileOnePtrs.DeadInventoryStart + 8 * characterId + 4, nameof(GetSpecialDeadInventory)),
        GameFile.FileTwo => ReadValue<byte>(FileTwoPtrs.DeadInventoryStart + 8 * characterId + 4, nameof(GetSpecialDeadInventory)),
        _ => 0xFF
    };

    public ushort GetBleedTime(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<ushort>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.BleedTimeOffset, nameof(GetBleedTime)),
        GameFile.FileTwo => ReadValue<ushort>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.BleedTimeOffset, nameof(GetBleedTime)),
        _ => 0xFF
    };

    // TODO: fix this
    public ushort GetAntiVirusGTime(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<ushort>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.AntiVirusGTimeOffset, nameof(GetAntiVirusGTime)),
        GameFile.FileTwo => ReadValue<ushort>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.AntiVirusGTimeOffset, nameof(GetAntiVirusGTime)),
        _ => 0xFF
    };

    public ushort GetHerbTime(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<ushort>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.HerbTimeOffset, nameof(GetHerbTime)),
        GameFile.FileTwo => ReadValue<ushort>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.HerbTimeOffset, nameof(GetHerbTime)),
        _ => 0xFF
    };

    public ushort GetAntiVirusTime(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<ushort>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.AntiVirusTimeOffset, nameof(GetAntiVirusTime)),
        GameFile.FileTwo => ReadValue<ushort>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.AntiVirusTimeOffset, nameof(GetAntiVirusTime)),
        _ => 0xFF
    };

    public float GetPower(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<float>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.PowerOffset, nameof(GetPower)),
        GameFile.FileTwo => ReadValue<float>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.PowerOffset, nameof(GetPower)),
        _ => 0xFF
    };

    public float GetSize(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<float>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.SizeOffset, nameof(GetSize)),
        GameFile.FileTwo => ReadValue<float>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.SizeOffset, nameof(GetSize)),
        _ => 0xFF
    };

    public float GetSpeed(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<float>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.SpeedOffset, nameof(GetSpeed)),
        GameFile.FileTwo => ReadValue<float>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.SpeedOffset, nameof(GetSpeed)),
        _ => 0xFF
    };

    public float GetPositionX(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<float>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.PositionXOffset, nameof(GetPositionX)),
        GameFile.FileTwo => ReadValue<float>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.PositionXOffset, nameof(GetPositionX)),
        _ => 0xFF
    };

    public float GetPositionY(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<float>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.PositionYOffset, nameof(GetPositionY)),
        GameFile.FileTwo => ReadValue<float>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.PositionYOffset, nameof(GetPositionY)),
        _ => 0xFF
    };

    public int GetCurVirus(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<int>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.VirusOffset, nameof(GetCurVirus)),
        GameFile.FileTwo => ReadValue<int>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.VirusOffset, nameof(GetCurVirus)),
        _ => 0xFF
    };

    public int GetMaxVirus(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<int>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.VirusOffset + 4, nameof(GetMaxVirus)),
        GameFile.FileTwo => ReadValue<int>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.VirusOffset + 4, nameof(GetMaxVirus)),
        _ => 0xFF
    };

    public int GetMaxVirus(int characterId, byte characterType) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<int>(FileOnePtrs.VirusMaxStart + 4 * characterType, nameof(GetMaxVirus)),
        GameFile.FileTwo => ReadValue<int>(FileTwoPtrs.VirusMaxStart + 4 * characterType, nameof(GetMaxVirus)),
        _ => 0xFF
    };

    public float GetCritBonus(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<float>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.CritBonusOffset, nameof(GetCritBonus)),
        GameFile.FileTwo => ReadValue<float>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.CritBonusOffset, nameof(GetCritBonus)),
        _ => 0xFF
    };

    public byte GetNameId(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<byte>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.NameTypeOffset, nameof(GetNameId)),
        GameFile.FileTwo => ReadValue<byte>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.NameTypeOffset, nameof(GetNameId)),
        _ => 0xFF
    };

    public byte GetEquippedItem(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<byte>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.EquippedItemOffset, nameof(GetEquippedItem)),
        GameFile.FileTwo => ReadValue<byte>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.EquippedItemOffset, nameof(GetEquippedItem)),
        _ => 0xFF
    };

    public byte GetStatus(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<byte>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.CharacterStatusOffset, nameof(GetStatus)),
        GameFile.FileTwo => ReadValue<byte>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.CharacterStatusOffset, nameof(GetStatus)),
        _ => 0xFF
    };

    public string DecodeStatusText(byte status)
    {
        return status switch
        {
            0x00 => "OK",
            0x01 => "Poison",
            0x02 => "Bleed",
            0x03 => "Poison+Bleed",
            //0x2000 => "Loading" ??,
            _ => CurrentFile switch
            {
                GameFile.FileOne when status >= 0x80 => "Zombie",
                GameFile.FileOne when status is >= 0x18 and <= 0x1C => "Down",
                GameFile.FileOne when status > 0x1C => "Dead",
                GameFile.FileOne when status is 0x4 => "Cleared",
                GameFile.FileTwo when status is >= 0x30 and <= 0x34 => "Zombie",
                GameFile.FileTwo when status is >= 0x08 and < 0x0C => "Down",
                GameFile.FileTwo when status is 0x0C => "Down+Gas",
                GameFile.FileTwo when status > 0x0C => "Dead",
                GameFile.FileTwo when status is 0x04 => "Gas",
                GameFile.FileTwo when status is 0x06 => "Gas+Bleed",
                _ => $"unknown inGame character status: {status}"
            }
        };
    }

    public static string DecodeCondition(short curHP, short maxHP)
    {
        return PercentageFormatter.GetPercentage(curHP, maxHP) switch
        {
            >= 75 => "fine",
            >= 50 => "caution",
            >= 25 => "caution2",
            > 0 => "danger",
            0 => "down",
            _ => string.Empty
        };
    }

    public DecodedInGamePlayer[] DecodedInGamePlayers { get; } = new DecodedInGamePlayer[Constants.MaxPlayers];

    public void UpdateInGamePlayers(bool debug = false)
    {
        if (CurrentFile is GameFile.Unknown) return;

        for (var i = 0; i < Constants.MaxPlayers; i++)
        {
            DecodedInGamePlayers[i].Enabled = GetIsEnabled(i);
            if (!DecodedInGamePlayers[i].Enabled) continue;

            DecodedInGamePlayers[i].InGame = GetIsInGame(i);
            DecodedInGamePlayers[i].CurrentHealthPoints = GetCurHealth(i);
            DecodedInGamePlayers[i].MaximumHealthPoints = GetMaxHealth(i);
            DecodedInGamePlayers[i].HealthPointsPercentage = PercentageFormatter.GetPercentage(
                DecodedInGamePlayers[i].CurrentHealthPoints, DecodedInGamePlayers[i].MaximumHealthPoints, decimalPlaces: 4);
            DecodedInGamePlayers[i].Size = GetSize(i);
            DecodedInGamePlayers[i].Speed = GetSpeed(i);
            DecodedInGamePlayers[i].Power = GetPower(i);
            DecodedInGamePlayers[i].PositionX = GetPositionX(i);
            DecodedInGamePlayers[i].PositionY = GetPositionY(i);
            DecodedInGamePlayers[i].CharacterType = GetEnumString(GetType(i), CharacterBaseType.Unknown);
            DecodedInGamePlayers[i].NameId = GetNameId(i);
            DecodedInGamePlayers[i].CharacterName = DecodedInGamePlayers[i].NameId is 0 
                    ? DecodedInGamePlayers[i].CharacterType
                    : GetEnumString(DecodedInGamePlayers[i].NameId, CharacterNpcName.Unknown);
            DecodedInGamePlayers[i].Condition = DecodeCondition(DecodedInGamePlayers[i].CurrentHealthPoints, DecodedInGamePlayers[i].MaximumHealthPoints);
            DecodedInGamePlayers[i].Status = DecodeStatusText(GetStatus(i));
            DecodedInGamePlayers[i].CurVirus = GetCurVirus(i);
            DecodedInGamePlayers[i].MaxVirus = GetMaxVirus(i);
            DecodedInGamePlayers[i].VirusPercentage = PercentageFormatter.GetPercentage(
                DecodedInGamePlayers[i].CurVirus, DecodedInGamePlayers[i].MaxVirus, decimalPlaces: 4);
            // TODO: bugged?
            DecodedInGamePlayers[i].CritBonus = GetCritBonus(i);
            // TODO: Decode byte -> name -> icon/img
            DecodedInGamePlayers[i].SpecialItem = GetSpecialItem(i);
            DecodedInGamePlayers[i].EquippedItem = GetEquippedItem(i);
            DecodedInGamePlayers[i].RoomId = GetRoomId(i);

            // TODO: room can only be parsed if we know what scenario we are playing
            // var scenarioId = DataManager.GetScenarioId();
            // DecodedInGamePlayers[i].RoomName = DecodeRoomName(scenarioId, DecodedInGamePlayers[i].RoomId);

            // TODO: untested, formatting?
            DecodedInGamePlayers[i].BleedTime = GetBleedTime(i);
            DecodedInGamePlayers[i].AntiVirusTime = GetAntiVirusTime(i);
            DecodedInGamePlayers[i].AntiVirusGTime = GetAntiVirusGTime(i);
            DecodedInGamePlayers[i].HerbTime = GetHerbTime(i);

            // Players[i].CindyBag = Character.GetCindyBag(i);
            DecodedInGamePlayers[i].Inventory = GetInventory(i);
            DecodedInGamePlayers[i].SpecialInventory = GetSpecialInventory(i);
            DecodedInGamePlayers[i].DeadInventory = GetDeadInventory(i);
            DecodedInGamePlayers[i].SpecialDeadInventory = GetSpecialDeadInventory(i);
        }

        //if (debug) DataManLogger.Log(LogLevel.Debug, DecodedInGamePlayers.ToJson());
    }
}
