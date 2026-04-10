using System.Diagnostics.CodeAnalysis;

namespace OutbreakTracker2.Application.Services.Settings;

public sealed record OutbreakTrackerSettings
{
    public const string SectionName = "OutbreakTracker";

    public NotificationSettings Notifications { get; init; } = new();

    public AlertRuleSettings AlertRules { get; init; } = new();

    public bool TryValidate([NotNullWhen(false)] out string? error)
    {
        PlayerAlertRuleSettings players = AlertRules.Players;
        LobbyAlertRuleSettings lobby = AlertRules.Lobby;

        if (players.VirusWarningThreshold is < 0 or > 100)
        {
            error = "OutbreakTracker:AlertRules:Players:VirusWarningThreshold must be between 0 and 100.";
            return false;
        }

        if (players.VirusCriticalThreshold is < 0 or > 100)
        {
            error = "OutbreakTracker:AlertRules:Players:VirusCriticalThreshold must be between 0 and 100.";
            return false;
        }

        if (players.VirusWarningThreshold > players.VirusCriticalThreshold)
        {
            error =
                "OutbreakTracker:AlertRules:Players:VirusWarningThreshold cannot be greater than VirusCriticalThreshold.";
            return false;
        }

        if (lobby.NameMatchCreated && string.IsNullOrWhiteSpace(lobby.NameMatchFilter))
        {
            error =
                "OutbreakTracker:AlertRules:Lobby:NameMatchFilter cannot be empty when NameMatchCreated is enabled.";
            return false;
        }

        if (lobby.ScenarioMatchCreated && string.IsNullOrWhiteSpace(lobby.ScenarioMatchFilter))
        {
            error =
                "OutbreakTracker:AlertRules:Lobby:ScenarioMatchFilter cannot be empty when ScenarioMatchCreated is enabled.";
            return false;
        }

        error = null;
        return true;
    }
}
