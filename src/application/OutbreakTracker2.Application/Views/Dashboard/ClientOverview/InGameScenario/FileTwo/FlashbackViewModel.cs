using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Utility;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.FileTwo;

public sealed partial class FlashbackViewModel : ObservableObject
{
    [ObservableProperty]
    private string _flashbackTimeDisplay = string.Empty;

    public void Update(DecodedInGameScenario scenario)
    {
        if (!IsValidScenario(scenario.ScenarioName))
            return;

        FlashbackTimeDisplay = scenario.FlashbackTime is 0 or -1
            ? string.Empty
            : TimeUtility.GetTimeToString3(scenario.FlashbackTime);
    }

    private static bool IsValidScenario(string scenarioName) =>
        !string.IsNullOrEmpty(scenarioName) && scenarioName.Equals("Flashback", StringComparison.Ordinal);
}
