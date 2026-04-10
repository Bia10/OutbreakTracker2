using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Tracking;

internal static class DefaultEnemyAlertRules
{
    public static void Register(
        IEntityTracker<DecodedEnemy> enemies,
        IAppSettingsService settingsService,
        IDataManager dataManager
    )
    {
        enemies.AddAddedRule(
            new PredicateAlertRule<DecodedEnemy>(
                (cur, _) =>
                {
                    EnemyAlertRuleSettings settings = settingsService.Current.AlertRules.Enemies;
                    return settings.Spawned && cur.Enabled != 0 && cur.MaxHp > 0 && cur.SlotId > 0;
                },
                cur =>
                {
                    string roomName = ResolveRoomName(cur.RoomId, dataManager.InGameScenario.ScenarioName);
                    return new AlertNotification(
                        "Entity Spawned",
                        $"{cur.Name} spawned in {roomName}!",
                        AlertLevel.Info
                    );
                }
            )
        );

        enemies.AddRule(
            new PredicateAlertRule<DecodedEnemy>(
                (cur, prev) =>
                {
                    EnemyAlertRuleSettings settings = settingsService.Current.AlertRules.Enemies;
                    return settings.Spawned
                        && cur.Enabled != 0
                        && cur.MaxHp > 0
                        && cur.SlotId > 0
                        && (prev?.Enabled == 0);
                },
                cur =>
                {
                    string roomName = ResolveRoomName(cur.RoomId, dataManager.InGameScenario.ScenarioName);
                    return new AlertNotification(
                        "Entity Spawned",
                        $"{cur.Name} spawned in {roomName}!",
                        AlertLevel.Info
                    );
                }
            )
        );

        enemies.AddRule(
            new PredicateAlertRule<DecodedEnemy>(
                (cur, prev) =>
                {
                    EnemyAlertRuleSettings settings = settingsService.Current.AlertRules.Enemies;
                    return settings.Killed && cur.CurHp == 0 && (prev?.CurHp ?? 0) > 0 && prev?.Enabled != 0;
                },
                cur =>
                {
                    string roomName = ResolveRoomName(cur.RoomId, dataManager.InGameScenario.ScenarioName);
                    return new AlertNotification(
                        "Entity Killed",
                        $"{cur.Name} in {roomName} was killed!",
                        AlertLevel.Info
                    );
                }
            )
        );

        enemies.AddRemovedRule(
            new PredicateAlertRule<DecodedEnemy>(
                (cur, _) =>
                {
                    EnemyAlertRuleSettings settings = settingsService.Current.AlertRules.Enemies;
                    return settings.Killed && cur.CurHp <= 1;
                },
                cur =>
                {
                    string roomName = ResolveRoomName(cur.RoomId, dataManager.InGameScenario.ScenarioName);
                    return new AlertNotification(
                        "Entity Killed",
                        $"{cur.Name} in {roomName} was killed!",
                        AlertLevel.Info
                    );
                }
            )
        );

        enemies.AddRule(
            new PredicateAlertRule<DecodedEnemy>(
                (cur, prev) =>
                {
                    EnemyAlertRuleSettings settings = settingsService.Current.AlertRules.Enemies;
                    return settings.Despawned && cur.Enabled == 0 && (prev?.Enabled ?? 0) != 0;
                },
                cur =>
                {
                    string roomName = ResolveRoomName(cur.RoomId, dataManager.InGameScenario.ScenarioName);
                    return new AlertNotification(
                        "Entity Despawned",
                        $"{cur.Name} despawned from {roomName}.",
                        AlertLevel.Info
                    );
                }
            )
        );

        enemies.AddRule(
            new PredicateAlertRule<DecodedEnemy>(
                (cur, prev) =>
                {
                    EnemyAlertRuleSettings settings = settingsService.Current.AlertRules.Enemies;
                    return settings.RoomChange && cur.RoomId != (prev?.RoomId ?? cur.RoomId) && cur.Enabled != 0;
                },
                cur =>
                {
                    string roomName = ResolveRoomName(cur.RoomId, dataManager.InGameScenario.ScenarioName);
                    return new AlertNotification(
                        "Entity Room Change",
                        $"{cur.Name} moved to {roomName}.",
                        AlertLevel.Info
                    );
                }
            )
        );
    }

    private static string ResolveRoomName(byte roomId, string scenarioName) =>
        AlertRuleHelpers.ResolveRoomName(roomId, scenarioName);
}
