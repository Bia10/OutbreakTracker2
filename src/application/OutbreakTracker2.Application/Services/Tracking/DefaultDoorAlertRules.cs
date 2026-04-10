using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Tracking;

internal static class DefaultDoorAlertRules
{
    public static void Register(IEntityTracker<DecodedDoor> doors, IAppSettingsService settingsService)
    {
        doors.AddRule(
            new PredicateAlertRule<DecodedDoor>(
                (cur, prev) =>
                {
                    DoorAlertRuleSettings settings = settingsService.Current.AlertRules.Doors;
                    return settings.FlagChanged && cur.Flag != prev.Flag;
                },
                cur => new AlertNotification(
                    "Door Flag Changed",
                    $"Door state changed (flag: {cur.Flag}).",
                    AlertLevel.Info
                )
            )
        );

        doors.AddRule(
            new PredicateAlertRule<DecodedDoor>(
                (cur, prev) =>
                {
                    DoorAlertRuleSettings settings = settingsService.Current.AlertRules.Doors;
                    return settings.Destroyed && cur.Hp == 0 && prev.Hp > 0;
                },
                cur => new AlertNotification("Door Destroyed", "A door was destroyed!", AlertLevel.Warning)
            )
        );

        doors.AddRule(
            new PredicateAlertRule<DecodedDoor>(
                (cur, prev) =>
                {
                    DoorAlertRuleSettings settings = settingsService.Current.AlertRules.Doors;
                    return settings.StatusChanged && !string.Equals(cur.Status, prev.Status, StringComparison.Ordinal);
                },
                cur => new AlertNotification(
                    "Door Status Changed",
                    $"Door status is now {cur.Status}.",
                    AlertLevel.Info
                )
            )
        );
    }
}
