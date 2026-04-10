using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Tracking;

internal static class DefaultLobbySlotAlertRules
{
    public static void Register(IEntityTracker<DecodedLobbySlot> lobbySlots, IAppSettingsService settingsService)
    {
        lobbySlots.AddRule(
            new PredicateAlertRule<DecodedLobbySlot>(
                (cur, prev) =>
                {
                    LobbyAlertRuleSettings settings = settingsService.Current.AlertRules.Lobby;
                    return settings.GameCreated && IsNewActiveLobbyGame(cur, prev);
                },
                cur => new AlertNotification(
                    "Lobby Game Created",
                    $"Slot {cur.SlotNumber} \"{GetDisplayTitle(cur)}\" opened for {GetDisplayScenario(cur)}.",
                    AlertLevel.Info
                )
            )
        );

        lobbySlots.AddRule(
            new PredicateAlertRule<DecodedLobbySlot>(
                (cur, prev) =>
                {
                    LobbyAlertRuleSettings settings = settingsService.Current.AlertRules.Lobby;
                    return settings.NameMatchCreated
                        && IsNewActiveLobbyGame(cur, prev)
                        && MatchesFilter(cur.Title, settings.NameMatchFilter);
                },
                cur => new AlertNotification(
                    "Tracked Lobby Name Created",
                    $"Slot {cur.SlotNumber} \"{GetDisplayTitle(cur)}\" opened for {GetDisplayScenario(cur)} and matches the configured lobby name filter.",
                    AlertLevel.Info
                )
            )
        );

        lobbySlots.AddRule(
            new PredicateAlertRule<DecodedLobbySlot>(
                (cur, prev) =>
                {
                    LobbyAlertRuleSettings settings = settingsService.Current.AlertRules.Lobby;
                    return settings.ScenarioMatchCreated
                        && IsNewActiveLobbyGame(cur, prev)
                        && MatchesFilter(cur.ScenarioId, settings.ScenarioMatchFilter);
                },
                cur => new AlertNotification(
                    "Tracked Lobby Scenario Created",
                    $"Slot {cur.SlotNumber} \"{GetDisplayTitle(cur)}\" opened for {GetDisplayScenario(cur)} and matches the configured scenario filter.",
                    AlertLevel.Info
                )
            )
        );
    }

    private static bool IsNewActiveLobbyGame(DecodedLobbySlot cur, DecodedLobbySlot? prev) =>
        IsActiveLobbyGame(cur) && !IsActiveLobbyGame(prev);

    private static bool IsActiveLobbyGame(DecodedLobbySlot? slot) =>
        slot is not null
        && slot.SlotNumber >= 0
        && slot.MaxPlayers > 0
        && !string.IsNullOrWhiteSpace(slot.Title)
        && !string.IsNullOrWhiteSpace(slot.Status)
        && !string.Equals(slot.Status, "Unknown", StringComparison.Ordinal);

    private static bool MatchesFilter(string? value, string filter) =>
        !string.IsNullOrWhiteSpace(value)
        && !string.IsNullOrWhiteSpace(filter)
        && value.Contains(filter, StringComparison.OrdinalIgnoreCase);

    private static string GetDisplayTitle(DecodedLobbySlot slot) =>
        string.IsNullOrWhiteSpace(slot.Title) ? "Untitled Lobby" : slot.Title;

    private static string GetDisplayScenario(DecodedLobbySlot slot) =>
        string.IsNullOrWhiteSpace(slot.ScenarioId) ? "Unknown scenario" : slot.ScenarioId;
}
