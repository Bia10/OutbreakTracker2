using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGamePlayer;

public partial class PlayerConditionsViewModel : ObservableObject
{
    [ObservableProperty]
    private string conditionTitle = "Condition:";

    [ObservableProperty]
    private string conditionMessage = string.Empty;

    [ObservableProperty]
    private NotificationType conditionSeverity = NotificationType.Information;

    [ObservableProperty]
    private bool isConditionVisible;

    [ObservableProperty]
    private string statusTitle = "Status:";

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private NotificationType statusSeverity = NotificationType.Information;

    [ObservableProperty]
    private bool isStatusVisible;

    public void Update(string rawCondition, string rawStatus)
    {
        if (!string.IsNullOrEmpty(rawCondition))
        {
            ConditionTitle = "Condition:";
            ConditionMessage = rawCondition;
            ConditionSeverity = ConvertCondition(rawCondition);
            IsConditionVisible = true;
        }
        else
        {
            IsConditionVisible = false;
            ConditionTitle = string.Empty;
            ConditionMessage = string.Empty;
            ConditionSeverity = NotificationType.Information;
        }

        if (!string.IsNullOrEmpty(rawStatus))
        {
            StatusTitle = "Status:";
            StatusMessage = rawStatus;
            StatusSeverity = ConvertStatus(rawStatus);
            IsStatusVisible = true;
        }
        else
        {
            IsStatusVisible = false;
            StatusTitle = string.Empty;
            StatusMessage = string.Empty;
            StatusSeverity = NotificationType.Information;
        }
    }

    private static NotificationType ConvertCondition(string value)
    {
        return value.ToLower() switch
        {
            "fine" => NotificationType.Success,
            "caution2" => NotificationType.Warning,
            "caution" => NotificationType.Warning,
            "gas" => NotificationType.Warning,
            "danger" => NotificationType.Error,
            "down" => NotificationType.Error,
            "down+gas" => NotificationType.Error,
            "" => NotificationType.Error,
            _ => NotificationType.Error
        };
    }

    private static NotificationType ConvertStatus(string value)
    {
        return value switch
        {
            "OK" => NotificationType.Success,
            "Dead" => NotificationType.Error,
            "Zombie" => NotificationType.Error,
            "Down" => NotificationType.Warning,
            "Gas" => NotificationType.Warning,
            "Bleed" => NotificationType.Warning,
            "" => NotificationType.Error,
            _ => NotificationType.Error
        };
    }
}