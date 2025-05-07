using CommunityToolkit.Mvvm.ComponentModel;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGamePlayer;

public partial class PlayerConditionsViewModel : ObservableObject
{
    [ObservableProperty]
    private string conditionTitle = "Condition:";

    [ObservableProperty]
    private string conditionMessage = string.Empty;

    [ObservableProperty]
    private string statusTitle = "Status:";

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public void Update(string rawCondition, string rawStatus)
    {
            ConditionMessage = !string.IsNullOrEmpty(rawCondition) ? rawCondition : string.Empty;
            StatusMessage = !string.IsNullOrEmpty(rawStatus) ? rawStatus : string.Empty;
    }
}
