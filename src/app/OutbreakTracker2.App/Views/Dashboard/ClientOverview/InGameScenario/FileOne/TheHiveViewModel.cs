using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Outbreak.Models;
using System;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGameScenario.FileOne;

public partial class TheHiveViewModel : ObservableObject
{
    [ObservableProperty]
    private string _passHiveDisplay = string.Empty;

    private void Update(DecodedInGameScenario scenario)
    {
        if (!IsValidScenario(scenario.ScenarioName))
            return;

        PassHiveDisplay = CalculatePassHiveDisplay(scenario.Pass1);
    }

    private static string CalculatePassHiveDisplay(byte pass1)
    {
        return pass1 switch
        {
            > 0x00 and <= 0x1F or >= 0x80 and <= 0x9F => "3555-0930",
            >= 0x20 and <= 0x3F or >= 0x60 and <= 0x7F or >= 0xA0 and <= 0xBF or >= 0xE0 and < 0xFF => "5315-0930",
            >= 0x40 and <= 0x5F or >= 0xC0 and < 0xDF => "8211-0930",
            _ => $"Unrecognized Hive Pass1({pass1})"
        };
    }

    private static bool IsValidScenario(string scenarioName)
        => !string.IsNullOrEmpty(scenarioName)
           && scenarioName.Equals("the hive", StringComparison.Ordinal);
}