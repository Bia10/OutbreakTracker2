using Microsoft.Extensions.Logging;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Enums.Enemy;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Offsets;
using OutbreakTracker2.Outbreak.Utility;
using OutbreakTracker2.PCSX2.Client;
using OutbreakTracker2.PCSX2.EEmem;

namespace OutbreakTracker2.Outbreak.Readers;

public sealed class EnemiesReader : ReaderBase, IEnemiesReader
{
    public DecodedEnemy[] DecodedEnemies2 { get; private set; }

    public EnemiesReader(IGameClient gameClient, IEEmemAddressReader eememMemory, ILogger logger)
        : base(gameClient, eememMemory, logger)
    {
        DecodedEnemies2 = new DecodedEnemy[GameConstants.MaxEnemies2];
        for (int i = 0; i < GameConstants.MaxEnemies2; i++)
            DecodedEnemies2[i] = new DecodedEnemy();
    }

    private static byte GetBossType(byte nameId)
    {
        if (!EnumUtility.TryParseByValueOrMember(nameId, out EnemyType enemyType))
            return 0;

        return enemyType switch
        {
            EnemyType.Thanatos or EnemyType.Nyx or EnemyType.NyxTyrant or EnemyType.Tyrant => 2,
            EnemyType.Megabyte
            or EnemyType.GLeech
            or EnemyType.Leechman
            or EnemyType.GMutant
            or EnemyType.Titan
            or EnemyType.Lion
            or EnemyType.TyrantQuestion
            or EnemyType.NyxCore
            or EnemyType.Axeman
            or EnemyType.Megabytes
            or EnemyType.Gigabyte => 1,
            _ => 0,
        };
    }

    private static string GetEnemyName(byte nameId) => EnumUtility.GetEnumString(nameId, EnemyType.Unknown);

    private static string GetZombieName(byte typeId) => EnumUtility.GetEnumString(typeId, ZombieType.Unknown0);

    private static string GetDogName(byte typeId) => EnumUtility.GetEnumString(typeId, DogType.Unknown0);

    private static string GetScissorTailName(byte typeId) =>
        EnumUtility.GetEnumString(typeId, ScissorTailType.ScissorTail);

    private static string GetLionName(byte typeId) => EnumUtility.GetEnumString(typeId, LionType.Stalker);

    private static string GetThanatosName(byte typeId) => EnumUtility.GetEnumString(typeId, ThanatosType.Thanatos);

    private static string GetTyrantName(byte typeId) => EnumUtility.GetEnumString(typeId, TyrantType.Tyrant);

    public void UpdateEnemies2()
    {
        if (CurrentFile is GameFile.Unknown)
            return;

        nint listBase = CurrentFile == GameFile.FileOne ? FileOnePtrs.EnemyListOffset : FileTwoPtrs.EnemyListOffset;

        DecodedEnemy[] newDecodedEnemies2 = new DecodedEnemy[GameConstants.MaxEnemies2];

        int entrySize = GetFileSpecificSingleIntOffset(EnemyOffsets.EnemyListEntrySize);

        for (int i = 0; i < GameConstants.MaxEnemies2; i++)
        {
            int curMobOffset = entrySize * i;

            byte enabled = ReadValue<byte>(
                listBase,
                [curMobOffset + GetFileSpecificSingleNintOffset(EnemyOffsets.EnemyListEnabled)]
            );
            byte slotId = ReadValue<byte>(
                listBase,
                [curMobOffset + GetFileSpecificSingleNintOffset(EnemyOffsets.EnemyListSlotId)]
            );
            byte nameId = ReadValue<byte>(
                listBase,
                [curMobOffset + GetFileSpecificSingleNintOffset(EnemyOffsets.EnemyListNameId)]
            );
            byte typeId = ReadValue<byte>(
                listBase,
                [curMobOffset + GetFileSpecificSingleNintOffset(EnemyOffsets.EnemyListTypeId)]
            );
            ushort curHp = ReadValue<ushort>(
                listBase,
                [curMobOffset + GetFileSpecificSingleNintOffset(EnemyOffsets.EnemyListCurHp)]
            );
            ushort maxHp = ReadValue<ushort>(
                listBase,
                [curMobOffset + GetFileSpecificSingleNintOffset(EnemyOffsets.EnemyListMaxHp)]
            );
            byte roomId = ReadValue<byte>(
                listBase,
                [curMobOffset + GetFileSpecificSingleNintOffset(EnemyOffsets.EnemyListRoomId)]
            );
            byte status = ReadValue<byte>(
                listBase,
                [curMobOffset + GetFileSpecificSingleNintOffset(EnemyOffsets.EnemyListStatus)]
            );

            newDecodedEnemies2[i] = new DecodedEnemy
            {
                Id = GetPersistentUlidForEnemies2Slot(i),
                Enabled = enabled,
                InGame = slotId,
                SlotId = slotId,
                NameId = nameId,
                TypeId = typeId,
                CurHp = curHp,
                MaxHp = maxHp,
                RoomId = roomId,
                Status = status,
                BossType = GetBossType(nameId),
                Name = ResolveEnemyDisplayName(nameId, typeId),
            };
        }

        DecodedEnemies2 = newDecodedEnemies2;
    }

    private static string ResolveEnemyDisplayName(byte nameId, byte typeId)
    {
        if (typeId <= 1)
            return GetEnemyName(nameId);

        if (!EnumUtility.TryParseByValueOrMember(nameId, out EnemyType enemyType))
            return GetEnemyName(nameId);

        string enemyName = GetEnemyName(nameId);
        static string Prefer(string variant, string fallback) => string.IsNullOrEmpty(variant) ? fallback : variant;

        return enemyType switch
        {
            EnemyType.Zombie => Prefer(GetZombieName(typeId), enemyName),
            EnemyType.Dog => Prefer(GetDogName(typeId), enemyName),
            EnemyType.ScissorTail => Prefer(GetScissorTailName(typeId), enemyName),
            EnemyType.Lion => Prefer(GetLionName(typeId), enemyName),
            EnemyType.Tyrant => Prefer(GetTyrantName(typeId), enemyName),
            EnemyType.Thanatos => Prefer(GetThanatosName(typeId), enemyName),
            _ => enemyName,
        };
    }

    private readonly Dictionary<int, Ulid> _enemies2SlotUlids = [];

    private Ulid GetPersistentUlidForEnemies2Slot(int enemies2SlotIndex)
    {
        if (_enemies2SlotUlids.TryGetValue(enemies2SlotIndex, out Ulid ulid))
            return ulid;

        ulid = Ulid.NewUlid();
        _enemies2SlotUlids.Add(enemies2SlotIndex, ulid);

        return ulid;
    }
}
