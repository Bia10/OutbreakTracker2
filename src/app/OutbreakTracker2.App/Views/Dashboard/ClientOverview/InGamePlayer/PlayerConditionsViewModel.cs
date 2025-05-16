using CommunityToolkit.Mvvm.ComponentModel;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGamePlayer;

public partial class PlayerConditionsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _conditionTitle = "Condition:";

    [ObservableProperty]
    private string _conditionMessage = string.Empty;

    [ObservableProperty]
    private string _statusTitle = "Status:";

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public void Update(string rawCondition, string rawStatus)
    {
        ConditionMessage = !string.IsNullOrEmpty(rawCondition) ? rawCondition : string.Empty;
        StatusMessage = !string.IsNullOrEmpty(rawStatus) ? rawStatus : string.Empty;
    }
}
