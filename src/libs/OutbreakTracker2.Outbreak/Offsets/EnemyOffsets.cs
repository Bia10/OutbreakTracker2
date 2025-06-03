using OutbreakTracker2.Outbreak.Common;

namespace OutbreakTracker2.Outbreak.Offsets;

internal static class EnemyOffsets
{
    public static readonly (nint[] File1, nint[] File2) Enabled = ([FileOnePtrs.EnemyEnabled], [FileTwoPtrs.EnemyEnabled]);
    public static readonly (nint[] File1, nint[] File2) InGame = ([FileOnePtrs.EnemyInGame], [FileTwoPtrs.EnemyInGame]);
    public static readonly (nint[] File1, nint[] File2) NameId = ([FileOnePtrs.EnemyNameIdOffset], [FileTwoPtrs.EnemyNameIdOffset]);
    public static readonly (nint[] File1, nint[] File2) CurHealth = ([FileOnePtrs.EnemyHpOffset], [FileTwoPtrs.EnemyHpOffset]);
    public static readonly (nint[] File1, nint[] File2) MaxHealth = ([FileOnePtrs.EnemyMaxHpOffset], [FileTwoPtrs.EnemyMaxHpOffset]);
    public static readonly (nint[] File1, nint[] File2) Type = ([FileOnePtrs.EnemyTypeOffset], [FileTwoPtrs.EnemyTypeOffset]);
    public static readonly (nint[] File1, nint[] File2) EnemyListBase = ([FileOnePtrs.EnemyListOffset], [FileTwoPtrs.EnemyListOffset]);
    public static readonly (int File1, int File2) EnemyListEntrySize = new(FileOnePtrs.EnemyListEntrySize, FileTwoPtrs.EnemyListEntrySize);
    public static readonly (nint File1, nint File2) EnemyListSlotId = (0x1, 0x1);
    public static readonly (nint File1, nint File2) EnemyListNameId = (0x2, 0x2);
    public static readonly (nint File1, nint File2) EnemyListTypeId = (0x3, 0x3);
    public static readonly (nint File1, nint File2) EnemyListCurHp = (0x1C, 0x1C);
    public static readonly (nint File1, nint File2) EnemyListMaxHp = (0x1E, 0x1E);
    public static readonly (nint File1, nint File2) EnemyListRoomId = (0x22, 0x22);
    public static readonly (nint File1, nint File2) EnemyListStatus = (0x45, 0x45);
}