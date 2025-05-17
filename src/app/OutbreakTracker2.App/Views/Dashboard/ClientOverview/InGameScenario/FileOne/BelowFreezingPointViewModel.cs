using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Outbreak.Models;
using System;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGameScenario.FileOne;

public partial class BelowFreezingPointViewModel : ObservableObject
{
    [ObservableProperty]
    private string _pass1BelowFreezingPoint = string.Empty;

    [ObservableProperty]
    private string _pass2BelowFreezingPoint = string.Empty;

    [ObservableProperty]
    private string _passBelowFreezingPoint = string.Empty;

    private void Update(DecodedInGameScenario scenario)
    {
        if (!IsValidScenario(scenario.ScenarioName)) return;

        Pass1BelowFreezingPoint = CalculatePassBelowFreezingPointDisplay(scenario.Pass1);
        Pass2BelowFreezingPoint = CalculatePass2BelowFreezingPointDisplay(scenario.Pass2);
        PassBelowFreezingPoint = GetBelowFreezingPointPasswordDisplay();
    }

    private static string CalculatePassBelowFreezingPointDisplay(byte pass1)
    {
        return pass1 switch
        {
            > 0x00 and <= 0x1F or >= 0x80 and <= 0x9F => "0634",
            >= 0x20 and <= 0x3F or >= 0xA0 and <= 0xBF => "4509",
            >= 0x40 and <= 0x7F or >= 0xC0 and < 0xFF => "9741",
            _ => $"Unrecognized BFP Pass1({pass1})"
        };
    }

    private static string CalculatePass2BelowFreezingPointDisplay(byte pass2)
    {
        return pass2 switch
        {
            0x20 => "A375-B482",
            0x40 => "J126-D580",
            0x80 => "C582-A194",
            _ => $"Unrecognized BFP Pass2({pass2})"
        };
    }

    private string GetBelowFreezingPointPasswordDisplay()
        => $"{Pass1BelowFreezingPoint}-{Pass2BelowFreezingPoint}";

    private static bool IsValidScenario(string scenarioName)
        => !string.IsNullOrEmpty(scenarioName)
           && scenarioName.Equals("below freezing point", StringComparison.Ordinal);
}