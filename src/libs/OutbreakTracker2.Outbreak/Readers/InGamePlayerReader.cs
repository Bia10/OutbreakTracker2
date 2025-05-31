using Microsoft.Extensions.Logging;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Enums.Character;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Offsets;
using OutbreakTracker2.Outbreak.Serialization;
using OutbreakTracker2.Outbreak.Utility;
using OutbreakTracker2.PCSX2.Client;
using OutbreakTracker2.PCSX2.EEmem;
using System.Text.Json;

namespace OutbreakTracker2.Outbreak.Readers;

public sealed class InGamePlayerReader : ReaderBase
{
    public DecodedInGamePlayer[] DecodedInGamePlayers { get; private set; }

    public InGamePlayerReader(GameClient gameClient, IEEmemMemory eememMemory, ILogger logger)
        : base(gameClient, eememMemory, logger)
    {
        DecodedInGamePlayers = new DecodedInGamePlayer[GameConstants.MaxPlayers];
        for (int i = 0; i < GameConstants.MaxPlayers; i++)
            DecodedInGamePlayers[i] = new DecodedInGamePlayer();
    }

    private nint GetInGamePlayerBasePointer(int characterId)
    {
        return CurrentFile switch
        {
            GameFile.FileOne => FileOnePtrs.GetPlayerStartAddress(characterId),
            GameFile.FileTwo => FileTwoPtrs.GetPlayerStartAddress(characterId),
            _ => nint.Zero
        };
    }

    private bool GetIsEnabled(int characterId)
    {
        nint basePlayerAddress = GetInGamePlayerBasePointer(characterId);
        ReadOnlySpan<nint> offsets = GetFileSpecificOffsets(InGamePlayerOffsets.CharacterEnabledOffset);
        return ReadValue<bool>(basePlayerAddress, offsets);
    }

    private bool GetIsInGame(int characterId)
    {
        nint basePlayerAddress = GetInGamePlayerBasePointer(characterId);
        ReadOnlySpan<nint> offsets = GetFileSpecificOffsets(InGamePlayerOffsets.CharacterInGameOffset);
        return ReadValue<bool>(basePlayerAddress, offsets);
    }

    private short GetRoomId(int characterId)
    {
        nint basePlayerAddress = GetInGamePlayerBasePointer(characterId);
        ReadOnlySpan<nint> offsets = GetFileSpecificOffsets(InGamePlayerOffsets.RoomIdOffset);
        return ReadValue<short>(basePlayerAddress, offsets);
    }

    private short GetCurHealth(int characterId)
    {
        nint basePlayerAddress = GetInGamePlayerBasePointer(characterId);
        ReadOnlySpan<nint> offsets = GetFileSpecificOffsets(InGamePlayerOffsets.CurHpOffset);
        return ReadValue<short>(basePlayerAddress, offsets);
    }

    private short GetMaxHealth(int characterId)
    {
        nint basePlayerAddress = GetInGamePlayerBasePointer(characterId);
        ReadOnlySpan<nint> offsets = GetFileSpecificOffsets(InGamePlayerOffsets.MaxHpOffset);
        return ReadValue<short>(basePlayerAddress, offsets);
    }

    private byte GetCharacterType(int characterId)
    {
        nint basePlayerAddress = GetInGamePlayerBasePointer(characterId);
        ReadOnlySpan<nint> offsets = GetFileSpecificOffsets(InGamePlayerOffsets.CharacterTypeOffset);
        return ReadValue<byte>(basePlayerAddress, offsets);
    }

    private byte GetEquippedItem(int characterId)
    {
        nint basePlayerAddress = GetInGamePlayerBasePointer(characterId);
        ReadOnlySpan<nint> offsets = GetFileSpecificOffsets(InGamePlayerOffsets.EquippedItemOffset);
        return ReadValue<byte>(basePlayerAddress, offsets);
    }

    private byte[] GetInventory(int characterId)
    {
        byte[] inventory = new byte[4];
        nint basePlayerAddress = GetInGamePlayerBasePointer(characterId);
        for (int i = 0; i < 4; i++)
        {
            nint baseInventoryOffset = CurrentFile switch
            {
                GameFile.FileOne => InGamePlayerOffsets.InventoryOffset.File1[0],
                GameFile.FileTwo => InGamePlayerOffsets.InventoryOffset.File2[0],
                _ => nint.Zero
            };

            ReadOnlySpan<nint> offsets = [baseInventoryOffset + i];
            inventory[i] = ReadValue<byte>(basePlayerAddress, offsets);
        }

        return inventory;
    }

    private byte GetSpecialItem(int characterId)
    {
        nint basePlayerAddress = GetInGamePlayerBasePointer(characterId);
        nint baseInventoryOffset = CurrentFile switch
        {
            GameFile.FileOne => InGamePlayerOffsets.InventoryOffset.File1[0],
            GameFile.FileTwo => InGamePlayerOffsets.InventoryOffset.File2[0],
            _ => nint.Zero
        };

        ReadOnlySpan<nint> offsets = [baseInventoryOffset + 4];
        return ReadValue<byte>(basePlayerAddress, offsets);
    }

    private byte[] GetSpecialInventory(int characterId)
    {
        byte[] specialInventory = new byte[4];
        nint basePlayerAddress = GetInGamePlayerBasePointer(characterId);
        for (int i = 0; i < 4; i++)
        {
            nint baseInventoryOffset = CurrentFile switch
            {
                GameFile.FileOne => InGamePlayerOffsets.InventoryOffset.File1[0],
                GameFile.FileTwo => InGamePlayerOffsets.InventoryOffset.File2[0],
                _ => nint.Zero
            };

            ReadOnlySpan<nint> offsets = [baseInventoryOffset + 5 + i];
            specialInventory[i] = ReadValue<byte>(basePlayerAddress, offsets);
        }

        return specialInventory;
    }

    private byte[] GetDeadInventory(int characterId)
    {
        byte[] deadInventory = new byte[4];
        for (int i = 0; i < 4; i++)
        {
            nint curItemOffset = (8 * characterId) + i;
            nint fileOneAddress = FileOnePtrs.DeadInventoryStart + curItemOffset;
            nint fileTwoAddress = FileTwoPtrs.DeadInventoryStart + curItemOffset;
            deadInventory[i] = ReadValue<byte>([fileOneAddress], [fileTwoAddress], errorValue: 0);
        }

        return deadInventory;
    }

    private byte[] GetSpecialDeadInventory(int characterId)
    {
        byte[] specialDeadInventory = new byte[4];
        for (int i = 0; i < 4; i++)
        {
            nint curItemOffset = (8 * characterId) + 4 + i;
            nint fileOneAddress = FileOnePtrs.DeadInventoryStart + curItemOffset;
            nint fileTwoAddress = FileTwoPtrs.DeadInventoryStart + curItemOffset;
            specialDeadInventory[i] = ReadValue<byte>([fileOneAddress], [fileTwoAddress], errorValue: 0);
        }

        return specialDeadInventory;
    }

    private ushort GetBleedTime(int characterId)
    {
        nint basePlayerAddress = GetInGamePlayerBasePointer(characterId);
        ReadOnlySpan<nint> offsets = GetFileSpecificOffsets(InGamePlayerOffsets.BleedTimeOffset);
        return ReadValue<ushort>(basePlayerAddress, offsets);
    }

    private ushort GetAntiVirusGTime(int characterId)
    {
        nint basePlayerAddress = GetInGamePlayerBasePointer(characterId);
        ReadOnlySpan<nint> offsets = GetFileSpecificOffsets(InGamePlayerOffsets.AntiVirusGTimeOffset);
        return ReadValue<ushort>(basePlayerAddress, offsets);
    }

    private ushort GetHerbTime(int characterId)
    {
        nint basePlayerAddress = GetInGamePlayerBasePointer(characterId);
        ReadOnlySpan<nint> offsets = GetFileSpecificOffsets(InGamePlayerOffsets.HerbTimeOffset);
        return ReadValue<ushort>(basePlayerAddress, offsets);
    }

    private ushort GetAntiVirusTime(int characterId)
    {
        nint basePlayerAddress = GetInGamePlayerBasePointer(characterId);
        ReadOnlySpan<nint> offsets = GetFileSpecificOffsets(InGamePlayerOffsets.AntiVirusTimeOffset);
        return ReadValue<ushort>(basePlayerAddress, offsets);
    }

    private float GetPower(int characterId)
    {
        nint basePlayerAddress = GetInGamePlayerBasePointer(characterId);
        ReadOnlySpan<nint> offsets = GetFileSpecificOffsets(InGamePlayerOffsets.PowerOffset);
        return ReadValue<float>(basePlayerAddress, offsets);
    }

    private float GetSize(int characterId)
    {
        nint basePlayerAddress = GetInGamePlayerBasePointer(characterId);
        ReadOnlySpan<nint> offsets = GetFileSpecificOffsets(InGamePlayerOffsets.SizeOffset);
        return ReadValue<float>(basePlayerAddress, offsets);
    }

    private float GetSpeed(int characterId)
    {
        nint basePlayerAddress = GetInGamePlayerBasePointer(characterId);
        ReadOnlySpan<nint> offsets = GetFileSpecificOffsets(InGamePlayerOffsets.SpeedOffset);
        return ReadValue<float>(basePlayerAddress, offsets);
    }

    private float GetPositionX(int characterId)
    {
        nint basePlayerAddress = GetInGamePlayerBasePointer(characterId);
        ReadOnlySpan<nint> offsets = GetFileSpecificOffsets(InGamePlayerOffsets.PositionXOffset);
        return ReadValue<float>(basePlayerAddress, offsets);
    }

    private float GetPositionY(int characterId)
    {
        nint basePlayerAddress = GetInGamePlayerBasePointer(characterId);
        ReadOnlySpan<nint> offsets = GetFileSpecificOffsets(InGamePlayerOffsets.PositionYOffset);
        return ReadValue<float>(basePlayerAddress, offsets);
    }

    private int GetCurVirus(int characterId)
    {
        nint basePlayerAddress = GetInGamePlayerBasePointer(characterId);
        ReadOnlySpan<nint> offsets = GetFileSpecificOffsets(InGamePlayerOffsets.VirusOffset);
        return ReadValue<int>(basePlayerAddress, offsets);
    }

    private int GetMaxVirus(int characterId)
    {
        int charTypeOffset = 4 * GetCharacterType(characterId);
        nint fileOneAddress = FileOnePtrs.VirusMaxStart + charTypeOffset;
        nint fileTwoAddress = FileTwoPtrs.VirusMaxStart + charTypeOffset;
        return ReadValue([fileOneAddress], [fileTwoAddress], errorValue: 0);
    }

    private float GetCritBonus(int characterId)
    {
        nint basePlayerAddress = GetInGamePlayerBasePointer(characterId);
        ReadOnlySpan<nint> offsets = GetFileSpecificOffsets(InGamePlayerOffsets.CritBonusOffset);
        return ReadValue<float>(basePlayerAddress, offsets);
    }

    private byte GetNameId(int characterId)
    {
        nint basePlayerAddress = GetInGamePlayerBasePointer(characterId);
        ReadOnlySpan<nint> offsets = GetFileSpecificOffsets(InGamePlayerOffsets.NameTypeOffset);
        return ReadValue<byte>(basePlayerAddress, offsets);
    }

    private byte GetStatus(int characterId)
    {
        nint basePlayerAddress = GetInGamePlayerBasePointer(characterId);
        ReadOnlySpan<nint> offsets = GetFileSpecificOffsets(InGamePlayerOffsets.CharacterStatusOffset);
        return ReadValue<byte>(basePlayerAddress, offsets);
    }

    private static string GetCharacterTypeFromTypeId(byte typeId)
        => EnumUtility.GetEnumString(typeId, CharacterBaseType.Unknown);

    private static string GetCharacterNpcNameFromNameId(byte nameId)
        => EnumUtility.GetEnumString(nameId, CharacterNpcName.Unknown);

    private static string GetCharacterName(byte nameId, string charType)
        => nameId is 0 ? charType : GetCharacterNpcNameFromNameId(nameId);

    private static double GetHealthPercentage(short curHealth, short maxHealth)
        => PercentageUtility.GetPercentage(curHealth, maxHealth, 3);

    private static double GetVirusPercentage(int curVirus, int maxVirus)
        => PercentageUtility.GetPercentage(curVirus, maxVirus, 3);

    private string DecodeStatusText(byte status)
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

    private static string DecodeCondition(short curHp, short maxHp)
    {
        return PercentageUtility.GetPercentage(curHp, maxHp) switch
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

        DecodedInGamePlayer[] newDecodedInGamePlayers = new DecodedInGamePlayer[GameConstants.MaxPlayers];

        for (int i = 0; i < GameConstants.MaxPlayers; i++)
        {
            if (GetIsEnabled(i) is false) continue;

            Ulid playerUlid = GetPersistentUlidForPlayerSlot(i);
            short curHealth = GetCurHealth(i);
            short maxHealth = GetMaxHealth(i);
            byte characterTypeId = GetCharacterType(i);
            string characterType = GetCharacterTypeFromTypeId(characterTypeId);
            int curVirus = GetCurVirus(i);
            int maxVirus = GetMaxVirus(i);
            byte nameId = GetNameId(i);
            byte status = GetStatus(i);

            newDecodedInGamePlayers[i] = new DecodedInGamePlayer
            {
                Id = playerUlid,
                IsEnabled = true,
                IsInGame = GetIsInGame(i),
                RoomId = GetRoomId(i),
                CurHealth = curHealth,
                MaxHealth = maxHealth,
                HealthPercentage = GetHealthPercentage(curHealth, maxHealth),
                Type = characterType,
                EquippedItem = GetEquippedItem(i),
                Inventory = GetInventory(i),
                SpecialItem = GetSpecialItem(i),
                SpecialInventory = GetSpecialInventory(i),
                DeadInventory = GetDeadInventory(i),
                SpecialDeadInventory = GetSpecialDeadInventory(i),
                BleedTime = GetBleedTime(i),
                AntiVirusGTime = GetAntiVirusGTime(i),
                HerbTime = GetHerbTime(i),
                AntiVirusTime = GetAntiVirusTime(i),
                Power = GetPower(i),
                Size = GetSize(i),
                Speed = GetSpeed(i),
                PositionX = GetPositionX(i),
                PositionY = GetPositionY(i),
                CurVirus = curVirus,
                MaxVirus = maxVirus,
                VirusPercentage = GetVirusPercentage(curVirus, maxVirus),
                CritBonus = GetCritBonus(i),
                Name = GetCharacterName(nameId, characterType),
                Status = DecodeStatusText(status),
                Condition = DecodeCondition(curHealth, maxHealth)
            };
        }

        DecodedInGamePlayers = newDecodedInGamePlayers;

        long duration = Environment.TickCount64 - start;

        if (!debug) return;

        Logger.LogDebug("Decoded in-game players in {Duration}ms", duration);
        foreach (string jsonObject in DecodedInGamePlayers.Select(player
                     => JsonSerializer.Serialize(player, DecodedInGamePlayersJsonContext.Default.DecodedInGamePlayer)))
            Logger.LogDebug("Decoded in-game player: {JsonObject}", jsonObject);
    }

    private readonly Dictionary<int, Ulid> _playerSlotUlids = new();

    private Ulid GetPersistentUlidForPlayerSlot(int playerSlotIndex)
    {
        if (_playerSlotUlids.TryGetValue(playerSlotIndex, out Ulid ulid))
            return ulid;

        ulid = Ulid.NewUlid();
        _playerSlotUlids.Add(playerSlotIndex, ulid);

        return ulid;
    }
}