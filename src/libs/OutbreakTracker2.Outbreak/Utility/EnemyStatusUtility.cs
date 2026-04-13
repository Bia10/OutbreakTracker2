using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums.Enemy;

namespace OutbreakTracker2.Outbreak.Utility;

public static class EnemyStatusUtility
{
    public static string GetHealthStatusForFileOne(int slotId, byte nameId, ushort curHp, ushort maxHp)
    {
        if (slotId is < 0 or >= GameConstants.MaxEnemies1)
            return $"Invalid enemy SlotId({slotId})";

        if (!EnumUtility.TryParseByValueOrMember(nameId, out EnemyType enemyType))
            return $"Failed to parse enemyType for nameId {nameId}";

        if (IsInvincibleHealth(enemyType, curHp, maxHp))
            return "Invincible";

        return curHp switch
        {
            0x0 or 0xffff or >= 0x8000 when enemyType is not (EnemyType.Mine or EnemyType.GasolineTank) => "Dead",
            0xffff when maxHp is 0x1 && enemyType is EnemyType.Mine => "Destroyed",
            0x0 when enemyType is EnemyType.GasolineTank => "Exploded",
            _ => $"{curHp}",
        };
    }

    public static string GetHealthStatusForFileTwo(int slotId, byte nameId, ushort curHp, ushort maxHp)
    {
        if (nameId == 0)
            return "Empty";

        if (!EnumUtility.TryParseByValueOrMember(nameId, out EnemyType enemyType))
            return $"Failed to parse enemyType for nameId {nameId}";

        if (IsInvincibleHealth(enemyType, curHp, maxHp))
            return "Invincible";

        return curHp switch
        {
            0x0 or 0xffff or >= 0x8000 when enemyType is not (EnemyType.Mine or EnemyType.GasolineTank) => "Dead",
            0xffff when maxHp is 0x1 && enemyType is EnemyType.Mine => "Destroyed",
            0x0 when enemyType is EnemyType.GasolineTank => "Exploded",
            _ => $"{curHp}",
        };
    }

    public static bool IsDeadStatus(string healthStatus) => healthStatus is "Dead" or "Destroyed" or "Exploded";

    private static bool IsInvincibleHealth(EnemyType enemyType, ushort curHp, ushort maxHp) =>
        curHp == 0x7fff
        || (enemyType is EnemyType.Megabytes && maxHp == 1)
        || enemyType
            is EnemyType.Drainer11
                or EnemyType.Drainer12
                or EnemyType.Drainer14
                or EnemyType.Neptune
                or EnemyType.Tentacles
                or EnemyType.LeechTentacles;
}
