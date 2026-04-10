using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Utility;

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
                        && MatchesScenario(cur.ScenarioId, settings.ScenarioMatchFilter);
                },
                cur => new AlertNotification(
                    "Tracked Lobby Scenario Created",
                    $"Slot {cur.SlotNumber} \"{GetDisplayTitle(cur)}\" opened for {GetDisplayScenario(cur)} and matches the configured scenario selection.",
                    AlertLevel.Info
                )
            )
        );
    }

    private static bool IsNewActiveLobbyGame(in DecodedLobbySlot cur, in DecodedLobbySlot? prev) =>
        IsActiveLobbyGame(cur) && !IsActiveLobbyGame(prev);

    private static bool IsActiveLobbyGame(in DecodedLobbySlot? slot) =>
        slot is { } s
        && s.SlotNumber >= 0
        && s.MaxPlayers > 0
        && !string.IsNullOrWhiteSpace(s.Title)
        && !string.IsNullOrWhiteSpace(s.Status)
        && !string.Equals(s.Status, "Unknown", StringComparison.Ordinal);

    private static bool MatchesFilter(string? value, string filter) =>
        !string.IsNullOrWhiteSpace(value)
        && !string.IsNullOrWhiteSpace(filter)
        && value.Contains(filter, StringComparison.OrdinalIgnoreCase);

    private static bool MatchesScenario(string? currentScenario, string configuredScenario)
    {
        if (string.IsNullOrWhiteSpace(currentScenario) || string.IsNullOrWhiteSpace(configuredScenario))
        {
            return false;
        }

        if (
            TryGetScenarioDisplayName(currentScenario, out string currentDisplayName)
            && TryGetScenarioDisplayName(configuredScenario, out string configuredDisplayName)
        )
        {
            return string.Equals(currentDisplayName, configuredDisplayName, StringComparison.OrdinalIgnoreCase);
        }

        return currentScenario.Contains(configuredScenario, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryGetScenarioDisplayName(string value, out string displayName)
    {
        if (EnumUtility.TryParseByValueOrMember(value, out Scenario scenario) && scenario != Scenario.Unknown)
        {
            displayName = EnumUtility.GetEnumString(scenario, Scenario.Unknown);
            return true;
        }

        displayName = string.Empty;
        return false;
    }

    private static string GetDisplayTitle(in DecodedLobbySlot slot) =>
        string.IsNullOrWhiteSpace(slot.Title) ? "Untitled Lobby" : slot.Title;

    private static string GetDisplayScenario(in DecodedLobbySlot slot) =>
        string.IsNullOrWhiteSpace(slot.ScenarioId) ? "Unknown scenario" : slot.ScenarioId;
}
