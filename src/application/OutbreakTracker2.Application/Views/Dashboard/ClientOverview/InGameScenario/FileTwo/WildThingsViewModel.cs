using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Utility;
using System;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.FileTwo;

public partial class WildThingsViewModel : ObservableObject
{
    // Used in "Wild Things"
    [ObservableProperty]
    private byte _coin;

    // Used in "Wild Things"
    [ObservableProperty]
    private short _wildThingsTime;

    // Used for Wild Things
    [ObservableProperty]
    private byte _passWildThings;

    // Computed properties
    [ObservableProperty]
    private string _wildThingsTimeDisplay = string.Empty;

    [ObservableProperty]
    private string _passWildThingsDisplay = string.Empty;

    public void Update(DecodedInGameScenario scenario)
    {
        if (!IsValidScenario(scenario.ScenarioName))
            return;

        Coin = scenario.Coin;
        WildThingsTime = scenario.WildThingsTime;
        PassWildThings = scenario.PassWildThings;

        WildThingsTimeDisplay = GetWildThingsTimeDisplay();
        PassWildThingsDisplay = CalculatePassWildThingsDisplay(scenario.GasRandom);
    }

    private string CalculatePassWildThingsDisplay(int gasRandom)
    {
        return (gasRandom % 16) switch
        {
            0 => "39DJ",
            1 => "LV4U",
            2 => "EXP2",
            3 => "E67C",
            4 => "6SR2",
            5 => "Q898",
            6 => "44V7",
            7 => "K3G6",
            8 => "SW4D",
            9 => "FM54",
            10 => "5TF3",
            11 => "4NZH",
            12 => "B37B",
            13 => "LYNX",
            14 => "9AAA",
            15 => "YTY7",
            _ => $"Unrecognized PassWildThings({PassWildThings}) "
        };
    }

    private static bool IsValidScenario(string scenarioName)
    {
        return !string.IsNullOrEmpty(scenarioName)
               && scenarioName.Equals("Wild things", StringComparison.Ordinal);
    }

    private string GetWildThingsTimeDisplay()
        => TimeUtility.GetTimeToString3(WildThingsTime);
}