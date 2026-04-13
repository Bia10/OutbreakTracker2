using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Tracking;

internal static class EnemyAlertRules
{
    public static void Register(
        IEntityTracker<DecodedEnemy> enemies,
        IAppSettingsService settingsService,
        ICurrentScenarioState scenarioState
    )
    {
        enemies.AddAddedRule(
            new PredicateAddedAlertRule<DecodedEnemy>(
                cur =>
                {
                    EnemyAlertRuleSettings settings = settingsService.Current.AlertRules.Enemies;
                    return settings.Spawned && cur.Enabled != 0 && cur.MaxHp > 0 && cur.SlotId > 0 && cur.RoomId != 0;
                },
                cur =>
                {
                    string roomName = ResolveRoomName(cur.RoomId, scenarioState.ScenarioName);
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
                        && cur.RoomId != 0
                        && prev.Enabled == 0;
                },
                cur =>
                {
                    string roomName = ResolveRoomName(cur.RoomId, scenarioState.ScenarioName);
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
                    return settings.Killed
                        && cur.CurHp == 0
                        && prev.CurHp > 0
                        && prev.Enabled != 0
                        && !AlertRuleHelpers.IsInvulnerableEnemy(cur.NameId, cur.MaxHp)
                        && scenarioState.Status == ScenarioStatus.InGame;
                },
                cur =>
                {
                    string roomName = ResolveRoomName(cur.RoomId, scenarioState.ScenarioName);
                    return new AlertNotification(
                        "Entity Killed",
                        $"{cur.Name} in {roomName} was killed!",
                        AlertLevel.Info
                    );
                }
            )
        );

        enemies.AddRemovedRule(
            new PredicateRemovedAlertRule<DecodedEnemy>(
                cur =>
                {
                    EnemyAlertRuleSettings settings = settingsService.Current.AlertRules.Enemies;
                    return settings.Killed
                        && cur.CurHp <= 1
                        && cur.RoomId != 0
                        && !AlertRuleHelpers.IsInvulnerableEnemy(cur.NameId, cur.MaxHp)
                        && scenarioState.Status == ScenarioStatus.InGame;
                },
                cur =>
                {
                    string roomName = ResolveRoomName(cur.RoomId, scenarioState.ScenarioName);
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
                    return settings.Despawned && cur.Enabled == 0 && prev.Enabled != 0;
                },
                cur =>
                {
                    string roomName = ResolveRoomName(cur.RoomId, scenarioState.ScenarioName);
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
                    return settings.RoomChange && cur.RoomId != prev.RoomId && cur.Enabled != 0;
                },
                cur =>
                {
                    string roomName = ResolveRoomName(cur.RoomId, scenarioState.ScenarioName);
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
