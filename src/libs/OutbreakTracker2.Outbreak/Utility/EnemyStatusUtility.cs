using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums.Enemy;

namespace OutbreakTracker2.Outbreak.Utility;

public static class EnemyStatusUtility
{
    public static string GetHealthStatusForFileOne(int slotId, byte nameId, ushort curHp, ushort maxHp) =>
        GetHealthStatusForFileOne(slotId, nameId, curHp, maxHp, string.Empty);

    public static string GetHealthStatusForFileOne(
        int slotId,
        byte nameId,
        ushort curHp,
        ushort maxHp,
        string entityName
    )
    {
        if (slotId is < 0 or >= GameConstants.MaxEnemies1)
            return $"Invalid enemy SlotId({slotId})";

        if (!EnumUtility.TryParseByValueOrMember(nameId, out EnemyType enemyType))
            return $"Failed to parse enemyType for nameId {nameId}";

        if (IsInvincibleHealth(enemyType, curHp, maxHp, entityName))
            return "Invincible";

        return curHp switch
        {
            0x0 or 0xffff or >= 0x8000 when !IsExplosiveEntity(enemyType, entityName) => "Dead",
            0xffff when maxHp is 0x1 && enemyType is EnemyType.Mine => "Destroyed",
            0x0 when IsExplosiveEntity(enemyType, entityName) => "Exploded",
            _ => $"{curHp}",
        };
    }

    public static string GetHealthStatusForFileTwo(int slotId, byte nameId, ushort curHp, ushort maxHp) =>
        GetHealthStatusForFileTwo(slotId, nameId, curHp, maxHp, string.Empty);

    public static string GetHealthStatusForFileTwo(
        int slotId,
        byte nameId,
        ushort curHp,
        ushort maxHp,
        string entityName
    )
    {
        if (IsEmptyFileTwoSlot(slotId, maxHp))
            return "Empty";

        if (!EnumUtility.TryParseByValueOrMember(nameId, out EnemyType enemyType))
            return $"Failed to parse enemyType for nameId {nameId}";

        if (IsInvincibleHealth(enemyType, curHp, maxHp, entityName))
            return "Invincible";

        return curHp switch
        {
            0x0 or 0xffff or >= 0x8000 when !IsExplosiveEntity(enemyType, entityName) => "Dead",
            0xffff when maxHp is 0x1 && enemyType is EnemyType.Mine => "Destroyed",
            0x0 when IsExplosiveEntity(enemyType, entityName) => "Exploded",
            _ => $"{curHp}",
        };
    }

    public static bool IsDeadStatus(string healthStatus) => healthStatus is "Dead" or "Destroyed" or "Exploded";

    private static bool IsEmptyFileTwoSlot(int slotId, ushort maxHp) => slotId <= 0 || maxHp == 0;

    private static bool IsInvincibleHealth(EnemyType enemyType, ushort curHp, ushort maxHp) =>
        IsInvincibleHealth(enemyType, curHp, maxHp, string.Empty);

    private static bool IsInvincibleHealth(EnemyType enemyType, ushort curHp, ushort maxHp, string entityName) =>
        curHp == 0x7fff
        || (enemyType is EnemyType.Fire && maxHp == 1)
        || (enemyType is EnemyType.Megabytes && maxHp == 1)
        || enemyType
            is EnemyType.Drainer11
                or EnemyType.Drainer12
                or EnemyType.Drainer14
                or EnemyType.Neptune
                or EnemyType.Tentacles
                or EnemyType.LeechTentacles;

    private static bool IsExplosiveEntity(EnemyType enemyType, string entityName)
    {
        if (enemyType is EnemyType.Mine or EnemyType.GasolineTank)
            return true;

        return IsExplosiveEntity(entityName);
    }

    private static bool IsExplosiveEntity(string entityName) =>
        !string.IsNullOrEmpty(entityName)
        && (
            entityName.Contains("canister", StringComparison.OrdinalIgnoreCase)
            || entityName.Contains("mine", StringComparison.OrdinalIgnoreCase)
            || entityName.Contains("explosive", StringComparison.OrdinalIgnoreCase)
            || entityName.Contains("fuel", StringComparison.OrdinalIgnoreCase)
        );
}
