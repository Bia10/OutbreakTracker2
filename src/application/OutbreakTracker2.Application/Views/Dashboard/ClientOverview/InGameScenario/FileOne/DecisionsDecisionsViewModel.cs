using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Outbreak.Models;
using System;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.FileOne;

public partial class DecisionsDecisionsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _decisionsDecisionsPassDisplay = string.Empty;

    [ObservableProperty]
    private string _decisionsDecisionsDisplay = string.Empty;

    [ObservableProperty]
    private string _clockTimeDisplay = string.Empty;

    public void Update(DecodedInGameScenario scenario)
    {
        if (!IsValidScenario(scenario.ScenarioName))
            return;

        DecisionsDecisionsPassDisplay = CalculateDecisionsDecisionsPassDisplay(scenario.Pass3, scenario.Pass6);
        ClockTimeDisplay = CalculateClockTimeDisplay(scenario.Difficulty);
        DecisionsDecisionsDisplay = GetDecisionsDecisionsDisplay();
    }

    private static string CalculateClockTimeDisplay(string difficulty)
    {
        return difficulty.ToLowerInvariant() switch
        {
            // TODO: this looks kinda weird easy doesn't seem so easy at all
            "easy" => "03:25",
            "normal" => "10:05",
            "hard" => "07:40",
            "very hard" => "02:50",
            _ => "N/A",
        };
    }

    private static string CalculateDecisionsDecisionsPassDisplay(byte pass3, byte pass6)
    {
        if (pass3 is > 0x00 and < 0x40 && pass6 % 2 == 0x0)
            return "4284";

        switch (pass3)
        {
            case >= 0x40 and < 0x80: return "4161";
            case >= 0x80 when pass6 == 0: return "4032";
        }

        if (pass3 is > 0x00 and < 0x40 && pass6 % 2 == 0x1)
            return "4927";

        return $"Unrecognized Decisions, decisions Pass3({pass3})";
    }

    private string GetDecisionsDecisionsDisplay()
        => $"{DecisionsDecisionsPassDisplay}-{ClockTimeDisplay}";

    private static bool IsValidScenario(string scenarioName)
        => !string.IsNullOrEmpty(scenarioName)
           && scenarioName.Equals("decisions, decisions", StringComparison.Ordinal);
}