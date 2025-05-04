using Microsoft.Extensions.Logging;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Enums.Character;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Serialization;
using OutbreakTracker2.Outbreak.Utility;
using OutbreakTracker2.PCSX2;
using System.Text.Json;

namespace OutbreakTracker2.Outbreak.Readers;

public class InGamePlayerReader : ReaderBase
{
    public DecodedInGamePlayer[] DecodedInGamePlayers { get; }

    public InGamePlayerReader(GameClient gameClient, EEmemMemory eememMemory, ILogger logger) : base(gameClient, eememMemory, logger)
    {
        DecodedInGamePlayers = new DecodedInGamePlayer[GameConstants.MaxPlayers];
        for (int i = 0; i < GameConstants.MaxPlayers; i++)
            DecodedInGamePlayers[i] = new DecodedInGamePlayer();
    }

    public bool GetIsEnabled(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<bool>(FileOnePtrs.GetPlayerStartAddress(characterId), [FileOnePtrs.CharacterEnabledOffset]),
        GameFile.FileTwo => ReadValue<bool>(FileTwoPtrs.GetPlayerStartAddress(characterId), [FileTwoPtrs.CharacterEnabledOffset]),
        _ => false
    };

    public bool GetIsInGame(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<bool>(FileOnePtrs.GetPlayerStartAddress(characterId), [FileOnePtrs.CharacterInGameOffset]),
        GameFile.FileTwo => ReadValue<bool>(FileTwoPtrs.GetPlayerStartAddress(characterId), [FileTwoPtrs.CharacterInGameOffset]),
        _ => false
    };

    public short GetRoomId(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<short>(FileOnePtrs.GetPlayerStartAddress(characterId), [FileOnePtrs.RoomIdOffset]),
        GameFile.FileTwo => ReadValue<short>(FileTwoPtrs.GetPlayerStartAddress(characterId), [FileTwoPtrs.RoomIdOffset]),
        _ => 0xFF
    };

    public short GetCurHealth(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<short>(FileOnePtrs.GetPlayerStartAddress(characterId), [FileOnePtrs.CurHpOffset]),
        GameFile.FileTwo => ReadValue<short>(FileTwoPtrs.GetPlayerStartAddress(characterId), [FileTwoPtrs.CurHpOffset]),
        _ => 0xFF
    };

    public short GetMaxHealth(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<short>(FileOnePtrs.GetPlayerStartAddress(characterId), [FileOnePtrs.MaxHpOffset]),
        GameFile.FileTwo => ReadValue<short>(FileTwoPtrs.GetPlayerStartAddress(characterId), [FileTwoPtrs.MaxHpOffset]),
        _ => 0xFF
    };

    public byte GetType(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<byte>(FileOnePtrs.GetPlayerStartAddress(characterId), [FileOnePtrs.CharacterTypeOffset]),
        GameFile.FileTwo => ReadValue<byte>(FileTwoPtrs.GetPlayerStartAddress(characterId), [FileTwoPtrs.CharacterTypeOffset]),
        _ => 0xFF
    };

    public byte GetEquippedItem(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<byte>(FileOnePtrs.GetPlayerStartAddress(characterId), [FileOnePtrs.EquippedItemOffset]),
        GameFile.FileTwo => ReadValue<byte>(FileTwoPtrs.GetPlayerStartAddress(characterId), [FileTwoPtrs.EquippedItemOffset]),
        _ => 0xFF
    };

    public byte[] GetInventory(int characterId)
    {
        byte[] inventory = new byte[4];
        for (int i = 0; i < 4; i++)
        {
            inventory[i] = CurrentFile switch
            {
                GameFile.FileOne => ReadValue<byte>(FileOnePtrs.GetPlayerStartAddress(characterId), [FileOnePtrs.InventoryOffset + i]),
                GameFile.FileTwo => ReadValue<byte>(FileTwoPtrs.GetPlayerStartAddress(characterId), [FileTwoPtrs.InventoryOffset + i]),
                _ => 0
            };
        }

        return inventory;
    }

    public byte GetSpecialItem(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<byte>(FileOnePtrs.GetPlayerStartAddress(characterId), [FileOnePtrs.InventoryOffset + 4]),
        GameFile.FileTwo => ReadValue<byte>(FileTwoPtrs.GetPlayerStartAddress(characterId), [FileTwoPtrs.InventoryOffset + 4]),
        _ => 0xFF
    };

    public byte[] GetSpecialInventory(int characterId)
    {
        var specialInventory = new byte[4];
        for (var i = 0; i < 4; i++)
        {
            nint curItemOffset;
            switch (CurrentFile)
            {
                case GameFile.FileOne:
                    curItemOffset = FileOnePtrs.InventoryOffset + 5 + i;
                    specialInventory[i] = ReadValue<byte>(FileOnePtrs.GetPlayerStartAddress(characterId), [curItemOffset]);
                    break;
                case GameFile.FileTwo:
                    curItemOffset = FileTwoPtrs.InventoryOffset + 5 + i;
                    specialInventory[i] = ReadValue<byte>(FileTwoPtrs.GetPlayerStartAddress(characterId), [curItemOffset]);
                    break;
                case GameFile.Unknown: break;
                default: specialInventory[i] = 0; break;
            }
        }

        return specialInventory;
    }

    public byte[] GetDeadInventory(int characterId)
    {
        var specialInventory = new byte[4];
        for (var i = 0; i < 4; i++)
        {
            nint curItemOffset = 8 * characterId + i;
            switch (CurrentFile)
            {
                case GameFile.FileOne:
                    specialInventory[i] = ReadValue<byte>(FileOnePtrs.DeadInventoryStart, [curItemOffset]);
                    break;
                case GameFile.FileTwo:
                    specialInventory[i] = ReadValue<byte>(FileTwoPtrs.DeadInventoryStart, [curItemOffset]);
                    break;
                case GameFile.Unknown: break;
                default: specialInventory[i] = 0; break;
            }
        }

        return specialInventory;
    }

    public byte[] GetSpecialDeadInventory(int characterId)
    {
        var specialInventory = new byte[4];
        for (var i = 0; i < 4; i++)
        {
            nint curItemOffset = 8 * characterId + 4 + i;
            switch (CurrentFile)
            {
                case GameFile.FileOne:
                    specialInventory[i] = ReadValue<byte>(FileOnePtrs.DeadInventoryStart, [curItemOffset]);
                    break;
                case GameFile.FileTwo:
                    specialInventory[i] = ReadValue<byte>(FileTwoPtrs.DeadInventoryStart, [curItemOffset]);
                    break;
                case GameFile.Unknown: break;
                default: specialInventory[i] = 0; break;
            }
        }

        return specialInventory;
    }

    public ushort GetBleedTime(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<ushort>(FileOnePtrs.GetPlayerStartAddress(characterId), [FileOnePtrs.BleedTimeOffset]),
        GameFile.FileTwo => ReadValue<ushort>(FileTwoPtrs.GetPlayerStartAddress(characterId), [FileTwoPtrs.BleedTimeOffset]),
        _ => 0xFF
    };

    // TODO: fix this
    public ushort GetAntiVirusGTime(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<ushort>(FileOnePtrs.GetPlayerStartAddress(characterId), [FileOnePtrs.AntiVirusGTimeOffset]),
        GameFile.FileTwo => ReadValue<ushort>(FileTwoPtrs.GetPlayerStartAddress(characterId), [FileTwoPtrs.AntiVirusGTimeOffset]),
        _ => 0xFF
    };

    public ushort GetHerbTime(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<ushort>(FileOnePtrs.GetPlayerStartAddress(characterId), [FileOnePtrs.HerbTimeOffset]),
        GameFile.FileTwo => ReadValue<ushort>(FileTwoPtrs.GetPlayerStartAddress(characterId), [FileTwoPtrs.HerbTimeOffset]),
        _ => 0xFF
    };

    public ushort GetAntiVirusTime(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<ushort>(FileOnePtrs.GetPlayerStartAddress(characterId), [FileOnePtrs.AntiVirusTimeOffset]),
        GameFile.FileTwo => ReadValue<ushort>(FileTwoPtrs.GetPlayerStartAddress(characterId), [FileTwoPtrs.AntiVirusTimeOffset]),
        _ => 0xFF
    };

    public float GetPower(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<float>(FileOnePtrs.GetPlayerStartAddress(characterId), [FileOnePtrs.PowerOffset]),
        GameFile.FileTwo => ReadValue<float>(FileTwoPtrs.GetPlayerStartAddress(characterId), [FileTwoPtrs.PowerOffset]),
        _ => 0xFF
    };

    public float GetSize(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<float>(FileOnePtrs.GetPlayerStartAddress(characterId), [FileOnePtrs.SizeOffset]),
        GameFile.FileTwo => ReadValue<float>(FileTwoPtrs.GetPlayerStartAddress(characterId), [FileTwoPtrs.SizeOffset]),
        _ => 0xFF
    };

    public float GetSpeed(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<float>(FileOnePtrs.GetPlayerStartAddress(characterId), [FileOnePtrs.SpeedOffset]),
        GameFile.FileTwo => ReadValue<float>(FileTwoPtrs.GetPlayerStartAddress(characterId), [FileTwoPtrs.SpeedOffset]),
        _ => 0xFF
    };

    public float GetPositionX(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<float>(FileOnePtrs.GetPlayerStartAddress(characterId), [FileOnePtrs.PositionXOffset]),
        GameFile.FileTwo => ReadValue<float>(FileTwoPtrs.GetPlayerStartAddress(characterId), [FileTwoPtrs.PositionXOffset]),
        _ => 0xFF
    };

    public float GetPositionY(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<float>(FileOnePtrs.GetPlayerStartAddress(characterId), [FileOnePtrs.PositionYOffset]),
        GameFile.FileTwo => ReadValue<float>(FileTwoPtrs.GetPlayerStartAddress(characterId), [FileTwoPtrs.PositionYOffset]),
        _ => 0xFF
    };

    public int GetCurVirus(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<int>(FileOnePtrs.GetPlayerStartAddress(characterId), [FileOnePtrs.VirusOffset]),
        GameFile.FileTwo => ReadValue<int>(FileTwoPtrs.GetPlayerStartAddress(characterId), [FileTwoPtrs.VirusOffset]),
        _ => 0xFF
    };

    public int GetMaxVirus(int characterId)
    {
        int charTypeOffset = 4 * GetType(characterId);

        return CurrentFile switch
        {
            GameFile.FileOne => ReadValue<int>(FileOnePtrs.VirusMaxStart, [charTypeOffset]),
            GameFile.FileTwo => ReadValue<int>(FileTwoPtrs.VirusMaxStart, [charTypeOffset]),
            _ => 0xFF
        };
    }

    public float GetCritBonus(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<float>(FileOnePtrs.GetPlayerStartAddress(characterId), [FileOnePtrs.CritBonusOffset]),
        GameFile.FileTwo => ReadValue<float>(FileTwoPtrs.GetPlayerStartAddress(characterId), [FileTwoPtrs.CritBonusOffset]),
        _ => 0xFF
    };

    public byte GetNameId(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<byte>(FileOnePtrs.GetPlayerStartAddress(characterId), [FileOnePtrs.NameTypeOffset]),
        GameFile.FileTwo => ReadValue<byte>(FileTwoPtrs.GetPlayerStartAddress(characterId), [FileTwoPtrs.NameTypeOffset]),
        _ => 0xFF
    };

    public byte GetStatus(int characterId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<byte>(FileOnePtrs.GetPlayerStartAddress(characterId), [FileOnePtrs.CharacterStatusOffset]),
        GameFile.FileTwo => ReadValue<byte>(FileTwoPtrs.GetPlayerStartAddress(characterId), [FileTwoPtrs.CharacterStatusOffset]),
        _ => 0xFF
    };

    public static string GetCharacterTypeFromTypeId(byte typeId)
        => EnumUtility.GetEnumString(typeId, CharacterBaseType.Unknown);

    public static string GetCharacterNpcNameFromNameId(byte nameId)
        => EnumUtility.GetEnumString(nameId, CharacterNpcName.Unknown);

    public static string GetCharacterName(byte nameId, string charType)
        => nameId is 0 ? charType : GetCharacterNpcNameFromNameId(nameId);

    public static double GetHealthPercentage(short curHealth, short maxHealth)
        => PercentageUtility.GetPercentage(curHealth, maxHealth, 3);

    public static double GetVirusPercentage(int curVirus, int maxVirus)
        => PercentageUtility.GetPercentage(curVirus, maxVirus, 3);

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
        return PercentageUtility.GetPercentage(curHP, maxHP) switch
        {
            >= 75 => "fine",
            >= 50 => "caution",
            >= 25 => "caution2",
            > 0 => "danger",
            0 => "down",
            _ => string.Empty
        };
    }

    public void UpdateInGamePlayers(bool debug = false)
    {
        if (CurrentFile is GameFile.Unknown) return;

        long start = Environment.TickCount64;

        if (debug) Logger.LogDebug("Decoding in-game players");

        for (var i = 0; i < GameConstants.MaxPlayers; i++)
        {
            DecodedInGamePlayers[i].Enabled = GetIsEnabled(i);
            if (!DecodedInGamePlayers[i].Enabled) continue;

            DecodedInGamePlayers[i].InGame = GetIsInGame(i);
            DecodedInGamePlayers[i].CurrentHealth = GetCurHealth(i);
            DecodedInGamePlayers[i].MaximumHealth = GetMaxHealth(i);
            DecodedInGamePlayers[i].HealthPercentage = GetHealthPercentage(DecodedInGamePlayers[i].CurrentHealth, DecodedInGamePlayers[i].MaximumHealth);
            DecodedInGamePlayers[i].Size = GetSize(i);
            DecodedInGamePlayers[i].Speed = GetSpeed(i);
            DecodedInGamePlayers[i].Power = GetPower(i);
            DecodedInGamePlayers[i].PositionX = GetPositionX(i);
            DecodedInGamePlayers[i].PositionY = GetPositionY(i);
            DecodedInGamePlayers[i].CharacterType = GetCharacterTypeFromTypeId(GetType(i));
            DecodedInGamePlayers[i].NameId = GetNameId(i);
            DecodedInGamePlayers[i].CharacterName = GetCharacterName(DecodedInGamePlayers[i].NameId, DecodedInGamePlayers[i].CharacterType);
            DecodedInGamePlayers[i].Condition = DecodeCondition(DecodedInGamePlayers[i].CurrentHealth, DecodedInGamePlayers[i].MaximumHealth);
            DecodedInGamePlayers[i].Status = DecodeStatusText(GetStatus(i));
            DecodedInGamePlayers[i].CurVirus = GetCurVirus(i);
            DecodedInGamePlayers[i].MaxVirus = GetMaxVirus(i);
            DecodedInGamePlayers[i].VirusPercentage = GetVirusPercentage(DecodedInGamePlayers[i].CurVirus, DecodedInGamePlayers[i].MaxVirus);

            // TODO: bugged?
            DecodedInGamePlayers[i].CritBonus = GetCritBonus(i);

            // TODO: Decode byte -> name -> icon/img
            DecodedInGamePlayers[i].SpecialItem = GetSpecialItem(i);
            DecodedInGamePlayers[i].EquippedItem = GetEquippedItem(i);
            DecodedInGamePlayers[i].RoomId = GetRoomId(i);

            // TODO: untested, formatting?
            DecodedInGamePlayers[i].BleedTime = GetBleedTime(i);
            DecodedInGamePlayers[i].AntiVirusTime = GetAntiVirusTime(i);
            DecodedInGamePlayers[i].AntiVirusGTime = GetAntiVirusGTime(i);
            DecodedInGamePlayers[i].HerbTime = GetHerbTime(i);

            DecodedInGamePlayers[i].Inventory = GetInventory(i);
            DecodedInGamePlayers[i].SpecialInventory = GetSpecialInventory(i);
            DecodedInGamePlayers[i].DeadInventory = GetDeadInventory(i);
            DecodedInGamePlayers[i].SpecialDeadInventory = GetSpecialDeadInventory(i);
        }

        long duration = Environment.TickCount64 - start;

        if (!debug) return;

        Logger.LogDebug("Decoded enemies2 in {Duration}ms", duration);
        foreach (string jsonObject in DecodedInGamePlayers.Select(inGamePlayer
                     => JsonSerializer.Serialize(inGamePlayer, DecodedInGamePlayersJsonContext.Default.DecodedInGamePlayer)))
            Logger.LogDebug("Decoded inGame player: {jsonObject}", jsonObject);
    }
}
