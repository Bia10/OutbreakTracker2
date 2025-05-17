using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Outbreak.Models;
using System;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGameScenario.FileTwo;

public partial class EndOfTheRoadViewModel : ObservableObject
{
    [ObservableProperty]
    private string _endOfRoadDisplay = string.Empty;

    private void Update(DecodedInGameScenario scenario)
    {
        if (!IsValidScenario(scenario.ScenarioName))
            return;

        EndOfRoadDisplay = GetEndOfRoadDisplay(scenario.Pass4);
    }

    private static string GetEndOfRoadDisplay(short pass4)
        => pass4.ToString();

    private static bool IsValidScenario(string scenarioName)
        => !string.IsNullOrEmpty(scenarioName)
           && scenarioName.Equals("end of the road", StringComparison.Ordinal);
}