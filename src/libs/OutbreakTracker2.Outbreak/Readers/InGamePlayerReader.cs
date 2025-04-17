using System.Text.Json;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Serialization;
using OutbreakTracker2.PCSX2;

namespace OutbreakTracker2.Outbreak.Readers;

public class InGamePlayerReader : ReaderBase
{
    public InGamePlayerReader(GameClient gameClient, EEmemMemory eememMemory) : base(gameClient, eememMemory)
    {
    }

    public bool GetIsEnabled(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<bool>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.CharacterEnabledOffset),
        GameFile.FileTwo => ReadValue<bool>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.CharacterEnabledOffset),
        _ => false
    };

    public bool GetIsInGame(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<bool>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.CharacterInGameOffset),
        GameFile.FileTwo => ReadValue<bool>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.CharacterInGameOffset),
        _ => false
    };

    public short GetRoomId(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<short>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.RoomIdOffset),
        GameFile.FileTwo => ReadValue<short>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.RoomIdOffset),
        _ => 0xFF
    };

    public short GetCurHealth(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<short>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.CurHpOffset),
        GameFile.FileTwo => ReadValue<short>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.CurHpOffset),
        _ => 0xFF
    };

    public short GetMaxHealth(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<short>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.MaxHpOffset),
        GameFile.FileTwo => ReadValue<short>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.MaxHpOffset),
        _ => 0xFF
    };

    public byte GetType(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<byte>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.CharacterTypeOffset),
        GameFile.FileTwo => ReadValue<byte>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.CharacterTypeOffset),
        _ => 0xFF
    };

    public byte GetInventory(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<byte>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.InventoryOffset),
        GameFile.FileTwo => ReadValue<byte>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.InventoryOffset),
        _ => 0xFF
    };

    public byte GetSpecialItem(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<byte>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.InventoryOffset + 4),
        GameFile.FileTwo => ReadValue<byte>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.InventoryOffset + 4),
        _ => 0xFF
    };

    public byte GetSpecialInventory(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<byte>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.InventoryOffset + 5, nameof(GetSpecialItem)),
        GameFile.FileTwo => ReadValue<byte>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.InventoryOffset + 5, nameof(GetSpecialItem)),
        _ => 0xFF
    };

    public byte GetDeadInventory(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<byte>(FileOnePtrs.DeadInventoryStart + 8 * characterId),
        GameFile.FileTwo => ReadValue<byte>(FileTwoPtrs.DeadInventoryStart + 8 * characterId),
        _ => 0xFF
    };

    public byte GetSpecialDeadInventory(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<byte>(FileOnePtrs.DeadInventoryStart + 8 * characterId + 4),
        GameFile.FileTwo => ReadValue<byte>(FileTwoPtrs.DeadInventoryStart + 8 * characterId + 4),
        _ => 0xFF
    };

    public ushort GetBleedTime(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<ushort>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.BleedTimeOffset),
        GameFile.FileTwo => ReadValue<ushort>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.BleedTimeOffset),
        _ => 0xFF
    };

    // TODO: fix this
    public ushort GetAntiVirusGTime(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<ushort>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.AntiVirusGTimeOffset),
        GameFile.FileTwo => ReadValue<ushort>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.AntiVirusGTimeOffset),
        _ => 0xFF
    };

    public ushort GetHerbTime(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<ushort>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.HerbTimeOffset),
        GameFile.FileTwo => ReadValue<ushort>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.HerbTimeOffset),
        _ => 0xFF
    };

    public ushort GetAntiVirusTime(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<ushort>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.AntiVirusTimeOffset),
        GameFile.FileTwo => ReadValue<ushort>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.AntiVirusTimeOffset),
        _ => 0xFF
    };

    public float GetPower(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<float>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.PowerOffset),
        GameFile.FileTwo => ReadValue<float>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.PowerOffset),
        _ => 0xFF
    };

    public float GetSize(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<float>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.SizeOffset),
        GameFile.FileTwo => ReadValue<float>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.SizeOffset),
        _ => 0xFF
    };

    public float GetSpeed(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<float>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.SpeedOffset),
        GameFile.FileTwo => ReadValue<float>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.SpeedOffset),
        _ => 0xFF
    };

    public float GetPositionX(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<float>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.PositionXOffset),
        GameFile.FileTwo => ReadValue<float>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.PositionXOffset),
        _ => 0xFF
    };

    public float GetPositionY(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<float>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.PositionYOffset),
        GameFile.FileTwo => ReadValue<float>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.PositionYOffset),
        _ => 0xFF
    };

    public int GetCurVirus(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<int>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.VirusOffset),
        GameFile.FileTwo => ReadValue<int>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.VirusOffset),
        _ => 0xFF
    };

    public int GetMaxVirus(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<int>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.VirusOffset + 4),
        GameFile.FileTwo => ReadValue<int>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.VirusOffset + 4),
        _ => 0xFF
    };

    public int GetMaxVirus(int characterId, byte characterType) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<int>(FileOnePtrs.VirusMaxStart + 4 * characterType),
        GameFile.FileTwo => ReadValue<int>(FileTwoPtrs.VirusMaxStart + 4 * characterType),
        _ => 0xFF
    };

    public float GetCritBonus(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<float>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.CritBonusOffset),
        GameFile.FileTwo => ReadValue<float>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.CritBonusOffset),
        _ => 0xFF
    };

    public byte GetNameId(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<byte>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.NameTypeOffset),
        GameFile.FileTwo => ReadValue<byte>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.NameTypeOffset),
        _ => 0xFF
    };

    public byte GetEquippedItem(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<byte>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.EquippedItemOffset),
        GameFile.FileTwo => ReadValue<byte>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.EquippedItemOffset),
        _ => 0xFF
    };

    public byte GetStatus(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<byte>(FileOnePtrs.GetPlayerStartAddress(characterId) + FileOnePtrs.CharacterStatusOffset),
        GameFile.FileTwo => ReadValue<byte>(FileTwoPtrs.GetPlayerStartAddress(characterId) + FileTwoPtrs.CharacterStatusOffset),
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

        long start = Environment.TickCount64;

        if (debug) Console.WriteLine("Decoding in-game players");

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

        long duration = Environment.TickCount64 - start;

        if (!debug) return;

        Console.WriteLine($"Decoded enemies2 in {duration}ms");

        foreach (DecodedInGamePlayer? inGamePlayer in DecodedInGamePlayers)
            Console.WriteLine(JsonSerializer.Serialize(inGamePlayer, DecodedInGamePlayersJsonContext.Default.DecodedInGamePlayer));
    }
}
