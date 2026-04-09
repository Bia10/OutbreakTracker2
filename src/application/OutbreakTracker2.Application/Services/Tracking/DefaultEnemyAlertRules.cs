using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Tracking;

internal static class DefaultEnemyAlertRules
{
    public static void Register(IEntityTracker<DecodedEnemy> enemies)
    {
        enemies.AddRule(
            new PredicateAlertRule<DecodedEnemy>(
                (cur, prev) => cur.Enabled != 0 && cur.MaxHp > 0 && cur.SlotId > 0 && (prev?.Enabled == 0),
                cur => new AlertNotification(
                    "Mob Spawned",
                    $"{cur.Name} spawned in room {cur.RoomId}!",
                    AlertLevel.Info
                )
            )
        );

        enemies.AddRule(
            new PredicateAlertRule<DecodedEnemy>(
                (cur, prev) => cur.CurHp == 0 && (prev?.CurHp ?? 0) > 0 && prev?.Enabled != 0,
                cur => new AlertNotification(
                    "Mob Killed",
                    $"{cur.Name} in room {cur.RoomId} was killed!",
                    AlertLevel.Info
                )
            )
        );

        enemies.AddRule(
            new PredicateAlertRule<DecodedEnemy>(
                (cur, prev) => cur.Enabled == 0 && (prev?.Enabled ?? 0) != 0,
                cur => new AlertNotification(
                    "Mob Despawned",
                    $"{cur.Name} despawned from room {cur.RoomId}.",
                    AlertLevel.Info
                )
            )
        );

        enemies.AddRule(
            new PredicateAlertRule<DecodedEnemy>(
                (cur, prev) => cur.RoomId != (prev?.RoomId ?? cur.RoomId) && cur.Enabled != 0,
                cur => new AlertNotification(
                    "Mob Room Change",
                    $"{cur.Name} moved to room {cur.RoomId}.",
                    AlertLevel.Info
                )
            )
        );
    }
}
