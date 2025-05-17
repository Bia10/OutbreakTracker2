using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Outbreak.Models;
using System;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGameScenario.FileOne;

public partial class HellfireViewModel : ObservableObject
{
    [ObservableProperty]
    private string _hellfirePassDisplay = string.Empty;

    [ObservableProperty]
    private string _hellfireMapDisplay = string.Empty;

    [ObservableProperty]
    private string _hellfirePowerDisplay = string.Empty;

    [ObservableProperty]
    private string _hellfireDisplay = string.Empty;

    private void Update(DecodedInGameScenario scenario)
    {
        if (!IsValidScenario(scenario.ScenarioName))
            return;

        HellfirePassDisplay = CalculateHellfirePassDisplay(scenario.Pass3);
        HellfireMapDisplay = CalculateHellfireMapDisplay(scenario.Pass4);
        HellfirePowerDisplay = CalculateHellfirePowerDisplay(scenario.PuzzleRandom);
        HellfireDisplay = GetHellfireDisplay();
    }

    private static string CalculateHellfirePassDisplay(byte pass3)
    {
        return pass3 switch
        {
            > 0x0 and <= 0x1 => "0721-DCH",
            >= 0x2 and <= 0x3 => "2287-JIA",
            >= 0x4 and <= 0x7 => "6354-BAE",
            >= 0x8 and <= 0xF => "5128-GGF",
            _ => $"Unrecognized Hellfire Pass3({pass3})"
        };
    }

    private static string CalculateHellfireMapDisplay(short pass4)
    {
        return pass4 switch
        {
            0x4000 => "1234",
            0x4020 => "234",
            0x4040 => "134",
            0x4060 => "34",
            0x4080 => "124",
            0x40A0 => "24",
            0x40C0 => "14",
            0x40E0 => "4",
            0x4100 => "123",
            0x4120 => "23",
            0x4140 => "13",
            0x4160 => "3",
            0x4180 => "12",
            0x41A0 => "2",
            0x41C0 => "1",
            _ => $"Unrecognized Hellfire Pass4({pass4})"
        };
    }

    private static string CalculateHellfirePowerDisplay(byte puzzleRandom)
        => puzzleRandom % 2 == 0 ? "1" : "2";

    private string GetHellfireDisplay()
        => $"{HellfirePassDisplay}-{HellfireMapDisplay}-{HellfirePowerDisplay}";

    private static bool IsValidScenario(string scenarioName)
        => !string.IsNullOrEmpty(scenarioName)
           && scenarioName.Equals("hellfire", StringComparison.Ordinal);
}