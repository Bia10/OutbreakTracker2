using Microsoft.Extensions.Logging;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Enums.Enemy;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Offsets;
using OutbreakTracker2.Outbreak.Serialization;
using OutbreakTracker2.Outbreak.Utility;
using OutbreakTracker2.PCSX2.Client;
using OutbreakTracker2.PCSX2.EEmem;
using System.Text.Json;

namespace OutbreakTracker2.Outbreak.Readers;

public sealed class EnemiesReader : ReaderBase
{
    public DecodedEnemy[] DecodedEnemies2 { get; private set; }

    public DecodedEnemy[] DecodedEnemies1 { get; private set; }

    public EnemiesReader(GameClient gameClient, IEEmemMemory eememMemory, ILogger logger)
        : base(gameClient, eememMemory, logger)
    {
        DecodedEnemies1 = new DecodedEnemy[GameConstants.MaxEnemies1];
        for (int i = 0; i < GameConstants.MaxEnemies1; i++)
            DecodedEnemies1[i] = new DecodedEnemy();

        DecodedEnemies2 = new DecodedEnemy[GameConstants.MaxEnemies2];
        for (int i = 0; i < GameConstants.MaxEnemies2; i++)
            DecodedEnemies2[i] = new DecodedEnemy();
    }

    private nint GetEnemyBaseAddressForCurrentFile(int enemyId)
    {
        return CurrentFile switch
        {
            GameFile.FileOne => FileOnePtrs.GetEnemyAddress(enemyId),
            GameFile.FileTwo => FileTwoPtrs.GetEnemyAddress(enemyId),
            _ => nint.Zero
        };
    }

    private byte GetNameId(nint baseAddress)
    {
        ReadOnlySpan<nint> offsets = GetFileSpecificOffsets(EnemyOffsets.NameId);
        return ReadValue<byte>(baseAddress, offsets);
    }

    private byte GetType(nint baseAddress)
    {
        ReadOnlySpan<nint> offsets = GetFileSpecificOffsets(EnemyOffsets.Type);
        return ReadValue<byte>(baseAddress, offsets);
    }

    private byte GetEnabled(nint baseAddress)
    {
        ReadOnlySpan<nint> offsets = GetFileSpecificOffsets(EnemyOffsets.Enabled);
        return ReadValue<byte>(baseAddress, offsets);
    }

    private byte GetInGame(nint baseAddress)
    {
        ReadOnlySpan<nint> offsets = GetFileSpecificOffsets(EnemyOffsets.InGame);
        return ReadValue<byte>(baseAddress, offsets);
    }

    private ushort GetCurHealth(nint baseAddress)
    {
        ReadOnlySpan<nint> offsets = GetFileSpecificOffsets(EnemyOffsets.CurHealth);
        return ReadValue<ushort>(baseAddress, offsets);
    }

    private ushort GetMaxHealth(nint baseAddress)
    {
        ReadOnlySpan<nint> offsets = GetFileSpecificOffsets(EnemyOffsets.MaxHealth);
        return ReadValue<ushort>(baseAddress, offsets);
    }

    private static string GetEnemyName(byte nameId)
        => EnumUtility.GetEnumString(nameId, EnemyType.Unknown);

    private static string GetZombieName(byte typeId)
        => EnumUtility.GetEnumString(typeId, ZombieType.Unknown0);

    private static string GetDogName(byte typeId)
        => EnumUtility.GetEnumString(typeId, DogType.Unknown0);

    private static string GetScissorTailName(byte typeId)
        => EnumUtility.GetEnumString(typeId, ScissorTailType.ScissorTail);

    private static string GetLionName(byte typeId)
        => EnumUtility.GetEnumString(typeId, LionType.Stalker);

    private static string GetThanatosName(byte typeId)
        => EnumUtility.GetEnumString(typeId, ThanatosType.Thanatos);

    private static string GetTyrantName(byte typeId)
        => EnumUtility.GetEnumString(typeId, TyrantType.Tyrant);

    public void UpdateEnemies()
    {
        DecodedEnemy[] newDecodedEnemies1 = new DecodedEnemy[GameConstants.MaxEnemies1];

        for (int i = 0; i < GameConstants.MaxEnemies1; i++)
        {
            nint enemyBaseAddress = GetEnemyBaseAddressForCurrentFile(i);

            newDecodedEnemies1[i] = new DecodedEnemy
            {
                Enabled = GetEnabled(enemyBaseAddress),
                InGame = GetInGame(enemyBaseAddress),
                CurHp = GetCurHealth(enemyBaseAddress),
                MaxHp = GetMaxHealth(enemyBaseAddress),
                TypeId = GetType(enemyBaseAddress),
                NameId = GetNameId(enemyBaseAddress)
            };
        }

        DecodedEnemies1 = newDecodedEnemies1;
    }

    public void UpdateEnemies2(bool debug = false)
    {
        if (CurrentFile is GameFile.Unknown) return;

        long start = Environment.TickCount64;

        if (debug) Logger.LogDebug("Decoding enemies2");

        DecodedEnemy[] newDecodedEnemies2 = new DecodedEnemy[GameConstants.MaxEnemies2];

        int entrySize = GetFileSpecificSingleIntOffset(EnemyOffsets.EnemyListEntrySize);

        for (int i = 0; i < GameConstants.MaxEnemies2; i++)
        {
            newDecodedEnemies2[i] = new DecodedEnemy();
            newDecodedEnemies2[i].Id = GetPersistentUlidForEnemies2Slot(i);

            int curMobOffset = entrySize * i;

            switch (CurrentFile)
            {
                case GameFile.FileOne:
                    newDecodedEnemies2[i].SlotId = ReadValue<byte>(FileOnePtrs.EnemyListOffset, [curMobOffset + 0x1]);
                    newDecodedEnemies2[i].NameId = ReadValue<byte>(FileOnePtrs.EnemyListOffset, [curMobOffset + 0x2]);
                    newDecodedEnemies2[i].TypeId = ReadValue<byte>(FileOnePtrs.EnemyListOffset, [curMobOffset + 0x3]);
                    newDecodedEnemies2[i].CurHp = ReadValue<ushort>(FileOnePtrs.EnemyListOffset, [curMobOffset + 0x1C]);
                    newDecodedEnemies2[i].MaxHp = ReadValue<ushort>(FileOnePtrs.EnemyListOffset, [curMobOffset + 0x1E]);
                    newDecodedEnemies2[i].RoomId = ReadValue<byte>(FileOnePtrs.EnemyListOffset, [curMobOffset + 0x22]);
                    newDecodedEnemies2[i].Status = ReadValue<byte>(FileOnePtrs.EnemyListOffset, [curMobOffset + 0x45]);
                    break;

                case GameFile.FileTwo:
                    newDecodedEnemies2[i].SlotId = ReadValue<byte>(FileTwoPtrs.EnemyListOffset, [curMobOffset + 0x1]);
                    newDecodedEnemies2[i].NameId = ReadValue<byte>(FileTwoPtrs.EnemyListOffset, [curMobOffset + 0x2]);
                    newDecodedEnemies2[i].TypeId = ReadValue<byte>(FileTwoPtrs.EnemyListOffset, [curMobOffset + 0x3]);
                    newDecodedEnemies2[i].CurHp = ReadValue<ushort>(FileTwoPtrs.EnemyListOffset, [curMobOffset + 0x1C]);
                    newDecodedEnemies2[i].MaxHp = ReadValue<ushort>(FileTwoPtrs.EnemyListOffset, [curMobOffset + 0x1E]);
                    newDecodedEnemies2[i].RoomId = ReadValue<byte>(FileTwoPtrs.EnemyListOffset, [curMobOffset + 0x22]);
                    newDecodedEnemies2[i].Status = ReadValue<byte>(FileTwoPtrs.EnemyListOffset, [curMobOffset + 0x45]);
                    break;

                case GameFile.Unknown: break;
                default: throw new ArgumentOutOfRangeException();
            }

            string enemyName = GetEnemyName(newDecodedEnemies2[i].NameId);
            string actualName = enemyName;

            if (newDecodedEnemies2[i].NameId <= 1 && newDecodedEnemies2[i].TypeId > 1)
            {
                string zombieName = GetZombieName(newDecodedEnemies2[i].TypeId);
                string dogName = GetDogName(newDecodedEnemies2[i].TypeId);
                string scissorTailName = GetScissorTailName(newDecodedEnemies2[i].TypeId);
                string lionName = GetLionName(newDecodedEnemies2[i].TypeId);
                string tyrantName = GetTyrantName(newDecodedEnemies2[i].TypeId);
                string thanatosName = GetThanatosName(newDecodedEnemies2[i].TypeId);

                actualName = actualName switch
                {
                    null => zombieName,
                    "Zombie" when !string.IsNullOrEmpty(zombieName) => zombieName,
                    "Dog" when !string.IsNullOrEmpty(dogName) => dogName,
                    "Sci.Tail" when !string.IsNullOrEmpty(scissorTailName) => scissorTailName,
                    "Lion" when !string.IsNullOrEmpty(lionName) => lionName,
                    "Tyrant" when !string.IsNullOrEmpty(tyrantName) => tyrantName,
                    "Thanatos" when !string.IsNullOrEmpty(thanatosName) => thanatosName,
                    _ => enemyName
                };
            }

            newDecodedEnemies2[i].Name = actualName;
        }

        DecodedEnemies2 = newDecodedEnemies2;

        long duration = Environment.TickCount64 - start;

        if (!debug) return;

        Logger.LogDebug("Decoded enemies2 in {Duration}ms", duration);
        foreach (string jsonObject in DecodedEnemies2.Select(enemy
                     => JsonSerializer.Serialize(enemy, DecodedEnemyJsonContext.Default.DecodedEnemy)))
            Logger.LogDebug("Decoded enemy: {JsonObject}", jsonObject);
    }

    private readonly Dictionary<int, Ulid> _enemies2SlotUlids = new();

    private Ulid GetPersistentUlidForEnemies2Slot(int enemies2SlotIndex)
    {
        if (_enemies2SlotUlids.TryGetValue(enemies2SlotIndex, out Ulid ulid))
            return ulid;

        ulid = Ulid.NewUlid();
        _enemies2SlotUlids.Add(enemies2SlotIndex, ulid);

        return ulid;
    }
}