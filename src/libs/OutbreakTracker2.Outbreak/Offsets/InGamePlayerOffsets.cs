using OutbreakTracker2.Outbreak.Common;

namespace OutbreakTracker2.Outbreak.Offsets;

internal static class InGamePlayerOffsets
{
    public static readonly (nint[] File1, nint[] File2) CharacterEnabledOffset = ([FileOnePtrs.CharacterEnabledOffset], [FileTwoPtrs.CharacterEnabledOffset]);
    public static readonly (nint[] File1, nint[] File2) CharacterInGameOffset = ([FileOnePtrs.CharacterInGameOffset], [FileTwoPtrs.CharacterInGameOffset]);
    public static readonly (nint[] File1, nint[] File2) RoomIdOffset = ([FileOnePtrs.RoomIdOffset], [FileTwoPtrs.RoomIdOffset]);
    public static readonly (nint[] File1, nint[] File2) CurHpOffset = ([FileOnePtrs.CurHpOffset], [FileTwoPtrs.CurHpOffset]);
    public static readonly (nint[] File1, nint[] File2) MaxHpOffset = ([FileOnePtrs.MaxHpOffset], [FileTwoPtrs.MaxHpOffset]);
    public static readonly (nint[] File1, nint[] File2) CharacterTypeOffset = ([FileOnePtrs.CharacterTypeOffset], [FileTwoPtrs.CharacterTypeOffset]);
    public static readonly (nint[] File1, nint[] File2) EquippedItemOffset = ([FileOnePtrs.EquippedItemOffset], [FileTwoPtrs.EquippedItemOffset]);
    public static readonly (nint[] File1, nint[] File2) InventoryOffset = ([FileOnePtrs.InventoryOffset], [FileTwoPtrs.InventoryOffset]);
    public static readonly (nint[] File1, nint[] File2) BleedTimeOffset = ([FileOnePtrs.BleedTimeOffset], [FileTwoPtrs.BleedTimeOffset]);
    public static readonly (nint[] File1, nint[] File2) AntiVirusGTimeOffset = ([FileOnePtrs.AntiVirusGTimeOffset], [FileTwoPtrs.AntiVirusGTimeOffset]);
    public static readonly (nint[] File1, nint[] File2) HerbTimeOffset = ([FileOnePtrs.HerbTimeOffset], [FileTwoPtrs.HerbTimeOffset]);
    public static readonly (nint[] File1, nint[] File2) AntiVirusTimeOffset = ([FileOnePtrs.AntiVirusTimeOffset], [FileTwoPtrs.AntiVirusTimeOffset]);
    public static readonly (nint[] File1, nint[] File2) PowerOffset = ([FileOnePtrs.PowerOffset], [FileTwoPtrs.PowerOffset]);
    public static readonly (nint[] File1, nint[] File2) SizeOffset = ([FileOnePtrs.SizeOffset], [FileTwoPtrs.SizeOffset]);
    public static readonly (nint[] File1, nint[] File2) SpeedOffset = ([FileOnePtrs.SpeedOffset], [FileTwoPtrs.SpeedOffset]);
    public static readonly (nint[] File1, nint[] File2) PositionXOffset = ([FileOnePtrs.PositionXOffset], [FileTwoPtrs.PositionXOffset]);
    public static readonly (nint[] File1, nint[] File2) PositionYOffset = ([FileOnePtrs.PositionYOffset], [FileTwoPtrs.PositionYOffset]);
    public static readonly (nint[] File1, nint[] File2) VirusOffset = ([FileOnePtrs.VirusOffset], [FileTwoPtrs.VirusOffset]);
    public static readonly (nint[] File1, nint[] File2) CritBonusOffset = ([FileOnePtrs.CritBonusOffset], [FileTwoPtrs.CritBonusOffset]);
    public static readonly (nint[] File1, nint[] File2) NameTypeOffset = ([FileOnePtrs.NameTypeOffset], [FileTwoPtrs.NameTypeOffset]);
    public static readonly (nint[] File1, nint[] File2) CharacterStatusOffset = ([FileOnePtrs.CharacterStatusOffset], [FileTwoPtrs.CharacterStatusOffset]);
    public static readonly (nint[] File1, nint[] File2) DeadInventoryStart = ([FileOnePtrs.DeadInventoryStart], [FileTwoPtrs.DeadInventoryStart]);
    public static readonly (nint[] File1, nint[] File2) VirusMaxStart = ([FileOnePtrs.VirusMaxStart], [FileTwoPtrs.VirusMaxStart]);
}