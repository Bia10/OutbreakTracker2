using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Enums.Enemy;
using OutbreakTracker2.Outbreak.Utility;

namespace OutbreakTracker2.Application.Services.Tracking;

internal static class AlertRuleHelpers
{
    internal static string ResolveRoomName(int roomId, string scenarioName)
    {
        if (
            !string.IsNullOrEmpty(scenarioName)
            && EnumUtility.TryParseByValueOrMember(scenarioName, out Scenario scenarioEnum)
        )
            return scenarioEnum.GetRoomName(roomId);

        return $"Room {roomId}";
    }

    /// <summary>
    /// Returns true for enemies that are structurally indestructible and can never be killed
    /// by player action. Most cases are type-based, but rolling Megabytes (Gigabyte projectiles
    /// in Showdown 3) are identified by MaxHp == 1 — normal Megabytes have real HP and are
    /// fully killable mini-bosses.
    /// </summary>
    internal static bool IsInvulnerableEnemy(byte nameId, ushort maxHp)
    {
        if (!EnumUtility.TryParseByValueOrMember(nameId, out EnemyType enemyType))
            return false;

        if (enemyType is EnemyType.Megabytes)
            return maxHp == 1;

        return enemyType
            is EnemyType.Drainer11
                or EnemyType.Drainer12
                or EnemyType.Drainer14
                or EnemyType.Neptune
                or EnemyType.Tentacles
                or EnemyType.LeechTentacles;
    }
}
