using System.Diagnostics.CodeAnalysis;

namespace OutbreakTracker2.Application.Services.Settings;

public sealed record OutbreakTrackerSettings
{
    public const string SectionName = "OutbreakTracker";

    public NotificationSettings Notifications { get; init; } = new();

    public DisplaySettings Display { get; init; } = new();

    public AlertRuleSettings AlertRules { get; init; } = new();

    public RunReportSettings RunReports { get; init; } = new();

    public DataManagerSettings DataManager { get; init; } = new();

    public bool TryValidate([NotNullWhen(false)] out string? error)
    {
        if (Notifications is null)
        {
            error = "OutbreakTracker:Notifications cannot be null.";
            return false;
        }

        if (Display is null)
        {
            error = "OutbreakTracker:Display cannot be null.";
            return false;
        }

        if (Display.EntitiesDock is null)
        {
            error = "OutbreakTracker:Display:EntitiesDock cannot be null.";
            return false;
        }

        if (Display.ScenarioItemsDock is null)
        {
            error = "OutbreakTracker:Display:ScenarioItemsDock cannot be null.";
            return false;
        }

        if (AlertRules is null)
        {
            error = "OutbreakTracker:AlertRules cannot be null.";
            return false;
        }

        if (RunReports is null)
        {
            error = "OutbreakTracker:RunReports cannot be null.";
            return false;
        }

        if (DataManager is null)
        {
            error = "OutbreakTracker:DataManager cannot be null.";
            return false;
        }

        if (AlertRules.Players is null)
        {
            error = "OutbreakTracker:AlertRules:Players cannot be null.";
            return false;
        }

        if (AlertRules.Enemies is null)
        {
            error = "OutbreakTracker:AlertRules:Enemies cannot be null.";
            return false;
        }

        if (AlertRules.Doors is null)
        {
            error = "OutbreakTracker:AlertRules:Doors cannot be null.";
            return false;
        }

        if (AlertRules.Lobby is null)
        {
            error = "OutbreakTracker:AlertRules:Lobby cannot be null.";
            return false;
        }

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

        if (DataManager.FastUpdateIntervalMs <= 0)
        {
            error = "OutbreakTracker:DataManager:FastUpdateIntervalMs must be greater than 0.";
            return false;
        }

        if (DataManager.SlowUpdateIntervalMs <= 0)
        {
            error = "OutbreakTracker:DataManager:SlowUpdateIntervalMs must be greater than 0.";
            return false;
        }

        error = null;
        return true;
    }
}
