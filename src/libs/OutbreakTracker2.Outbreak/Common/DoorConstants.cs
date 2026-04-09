using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace OutbreakTracker2.Outbreak.Common;

public static class DoorConstants
{
    private const ushort AlwaysUnlockedHp = 500;

    private static readonly FrozenSet<ushort> PassableFlags = new HashSet<ushort>
    {
        0,
        10,
        12,
        14,
        44,
        130,
    }.ToFrozenSet();

    private static readonly FrozenDictionary<ushort, string> DoorStatuses = new Dictionary<ushort, string>
    {
        [0] = "unlocked",
        [1] = "locked",
        [2] = "locked",
        [3] = "locked",
        [4] = "locked",
        [6] = "unknownState6",
        [8] = "locked",
        [10] = "unlocked",
        [12] = "unlocked",
        [13] = "unknownState13",
        [14] = "unlocked",
        [18] = "unknownState18",
        [44] = "unlocked",
        [130] = "unlocked",
        [2000] = "unlocked",
    }.ToFrozenDictionary();

    public static bool IsPassable(ushort hp, ushort flag) =>
        hp is AlwaysUnlockedHp or 0 || PassableFlags.Contains(flag);

    public static bool TryGetStatus(ushort hp, ushort flag, [NotNullWhen(true)] out string? status)
    {
        if (hp == AlwaysUnlockedHp)
        {
            status = "unlocked";
            return true;
        }

        return DoorStatuses.TryGetValue(flag, out status);
    }
}
