using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;
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

    private static byte GetBossType(byte nameId, ushort maxHp) => EnemyTypeDisplay.GetBossType(nameId, maxHp);

    private static string ResolveEnemyDisplayName(byte nameId, byte typeId) =>
        EnemyTypeDisplay.ResolveDisplayName(nameId, typeId);

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
                BossType = GetBossType(nameId, maxHp),
                Name = ResolveEnemyDisplayName(nameId, typeId),
            };
        }

        DecodedEnemies2 = newDecodedEnemies2;
    }

    private readonly ConcurrentDictionary<int, Ulid> _enemies2SlotUlids = [];

    private Ulid GetPersistentUlidForEnemies2Slot(int enemies2SlotIndex) =>
        _enemies2SlotUlids.GetOrAdd(enemies2SlotIndex, static _ => Ulid.NewUlid());
}
