using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Tracking;

internal static class DefaultPlayerAlertRules
{
    public static void Register(IEntityTracker<DecodedInGamePlayer> players, IAppSettingsService settingsService)
    {
        players.AddRule(
            new PredicateAlertRule<DecodedInGamePlayer>(
                (cur, prev) =>
                {
                    PlayerAlertRuleSettings settings = settingsService.Current.AlertRules.Players;
                    return settings.DangerCondition
                        && cur.Condition.Equals("danger", StringComparison.OrdinalIgnoreCase)
                        && !(prev?.Condition.Equals("danger", StringComparison.OrdinalIgnoreCase) ?? false);
                },
                cur => new AlertNotification("Condition Update", $"{cur.Name} is now in DANGER!", AlertLevel.Error)
            )
        );

        players.AddRule(
            new PredicateAlertRule<DecodedInGamePlayer>(
                (cur, prev) =>
                {
                    PlayerAlertRuleSettings settings = settingsService.Current.AlertRules.Players;
                    return settings.GasCondition
                        && cur.Condition.Equals("gas", StringComparison.OrdinalIgnoreCase)
                        && !(prev?.Condition.Equals("gas", StringComparison.OrdinalIgnoreCase) ?? false);
                },
                cur => new AlertNotification("Condition Update", $"{cur.Name} is gassed!", AlertLevel.Warning)
            )
        );

        players.AddRule(
            new PredicateAlertRule<DecodedInGamePlayer>(
                (cur, prev) =>
                {
                    PlayerAlertRuleSettings settings = settingsService.Current.AlertRules.Players;
                    return settings.DeadStatus
                        && string.Equals(cur.Status, "Dead", StringComparison.Ordinal)
                        && !string.Equals(prev?.Status, "Dead", StringComparison.Ordinal);
                },
                cur => new AlertNotification("Status Update", $"{cur.Name} has DIED!", AlertLevel.Error)
            )
        );

        players.AddRule(
            new PredicateAlertRule<DecodedInGamePlayer>(
                (cur, prev) =>
                {
                    PlayerAlertRuleSettings settings = settingsService.Current.AlertRules.Players;
                    return settings.ZombieStatus
                        && string.Equals(cur.Status, "Zombie", StringComparison.Ordinal)
                        && !string.Equals(prev?.Status, "Zombie", StringComparison.Ordinal);
                },
                cur => new AlertNotification("Status Update", $"{cur.Name} turned into a ZOMBIE!", AlertLevel.Error)
            )
        );

        players.AddRule(
            new PredicateAlertRule<DecodedInGamePlayer>(
                (cur, prev) =>
                {
                    PlayerAlertRuleSettings settings = settingsService.Current.AlertRules.Players;
                    return settings.DownStatus
                        && string.Equals(cur.Status, "Down", StringComparison.Ordinal)
                        && !string.Equals(prev?.Status, "Down", StringComparison.Ordinal);
                },
                cur => new AlertNotification("Status Update", $"{cur.Name} is now DOWNED!", AlertLevel.Warning)
            )
        );

        players.AddRule(
            new PredicateAlertRule<DecodedInGamePlayer>(
                (cur, prev) =>
                {
                    PlayerAlertRuleSettings settings = settingsService.Current.AlertRules.Players;
                    return settings.BleedStatus
                        && string.Equals(cur.Status, "Bleed", StringComparison.Ordinal)
                        && !string.Equals(prev?.Status, "Bleed", StringComparison.Ordinal);
                },
                cur => new AlertNotification("Status Update", $"{cur.Name} is now BLEEDING!", AlertLevel.Warning)
            )
        );

        players.AddRule(
            new PredicateAlertRule<DecodedInGamePlayer>(
                (cur, prev) =>
                {
                    PlayerAlertRuleSettings settings = settingsService.Current.AlertRules.Players;
                    return settings.HealthZero
                        && cur.CurHealth <= 0
                        && (prev?.CurHealth ?? 0) > 0
                        && !string.Equals(cur.Status, "Zombie", StringComparison.Ordinal);
                },
                cur => new AlertNotification("Player Died", $"{cur.Name} health dropped to 0!", AlertLevel.Error)
            )
        );

        players.AddRule(
            new PredicateAlertRule<DecodedInGamePlayer>(
                (cur, prev) =>
                {
                    PlayerAlertRuleSettings settings = settingsService.Current.AlertRules.Players;
                    return settings.VirusWarningEnabled
                        && cur.VirusPercentage >= settings.VirusWarningThreshold
                        && (prev?.VirusPercentage ?? 0.0) < settings.VirusWarningThreshold;
                },
                cur => new AlertNotification(
                    "Virus Warning",
                    $"{cur.Name} virus is at {cur.VirusPercentage:F1}%!",
                    AlertLevel.Warning
                )
            )
        );

        players.AddRule(
            new PredicateAlertRule<DecodedInGamePlayer>(
                (cur, prev) =>
                {
                    PlayerAlertRuleSettings settings = settingsService.Current.AlertRules.Players;
                    return settings.VirusCriticalEnabled
                        && cur.VirusPercentage >= settings.VirusCriticalThreshold
                        && (prev?.VirusPercentage ?? 0.0) < settings.VirusCriticalThreshold;
                },
                cur => new AlertNotification(
                    "Virus Critical",
                    $"{cur.Name} virus is at {cur.VirusPercentage:F1}%!",
                    AlertLevel.Error
                )
            )
        );

        players.AddRule(
            new PredicateAlertRule<DecodedInGamePlayer>(
                (cur, prev) =>
                {
                    PlayerAlertRuleSettings settings = settingsService.Current.AlertRules.Players;
                    return settings.AntiVirusExpired && cur.AntiVirusTime == 0 && (prev?.AntiVirusTime ?? 0) > 0;
                },
                cur => new AlertNotification(
                    "Antivirus Expired",
                    $"{cur.Name}'s antivirus ran out!",
                    AlertLevel.Warning
                )
            )
        );

        players.AddRule(
            new PredicateAlertRule<DecodedInGamePlayer>(
                (cur, prev) =>
                {
                    PlayerAlertRuleSettings settings = settingsService.Current.AlertRules.Players;
                    return settings.AntiVirusGExpired && cur.AntiVirusGTime == 0 && (prev?.AntiVirusGTime ?? 0) > 0;
                },
                cur => new AlertNotification(
                    "Antivirus-G Expired",
                    $"{cur.Name}'s antivirus-G ran out!",
                    AlertLevel.Warning
                )
            )
        );

        players.AddRule(
            new PredicateAlertRule<DecodedInGamePlayer>(
                (cur, prev) =>
                {
                    PlayerAlertRuleSettings settings = settingsService.Current.AlertRules.Players;
                    return settings.BleedStopped && cur.BleedTime == 0 && (prev?.BleedTime ?? 0) > 0;
                },
                cur => new AlertNotification("Bleed Stopped", $"{cur.Name}'s bleed timer expired.", AlertLevel.Info)
            )
        );

        players.AddRule(
            new PredicateAlertRule<DecodedInGamePlayer>(
                (cur, prev) =>
                {
                    PlayerAlertRuleSettings settings = settingsService.Current.AlertRules.Players;
                    return settings.RoomChange && cur.IsInGame && prev is not null && cur.RoomId != prev.RoomId;
                },
                cur => new AlertNotification("Room Change", $"{cur.Name} moved to room {cur.RoomId}.", AlertLevel.Info)
            )
        );

        players.AddRule(
            new PredicateAlertRule<DecodedInGamePlayer>(
                (cur, prev) =>
                {
                    PlayerAlertRuleSettings settings = settingsService.Current.AlertRules.Players;
                    return settings.Joined && cur.IsInGame && !(prev?.IsInGame ?? true);
                },
                cur => new AlertNotification("Player Joined", $"{cur.Name} joined the game.", AlertLevel.Info)
            )
        );

        players.AddRule(
            new PredicateAlertRule<DecodedInGamePlayer>(
                (cur, prev) =>
                {
                    PlayerAlertRuleSettings settings = settingsService.Current.AlertRules.Players;
                    return settings.Left && !cur.IsInGame && (prev?.IsInGame ?? false);
                },
                cur => new AlertNotification("Player Left", $"{cur.Name} left the game.", AlertLevel.Warning)
            )
        );
    }
}
