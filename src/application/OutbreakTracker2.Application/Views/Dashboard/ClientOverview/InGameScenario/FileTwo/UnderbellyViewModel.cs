using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Utility;
using System;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.FileTwo;

public partial class UnderbellyViewModel : ObservableObject
{
    [ObservableProperty]
    private short _passUnderbelly1;

    [ObservableProperty]
    private byte _passUnderbelly3;

    [ObservableProperty]
    private byte _passUnderbelly2;

    [ObservableProperty]
    private short _escapeTime;

    // Computed properties
    [ObservableProperty]
    private bool _isUnderbellyPasswordVisible;

    [ObservableProperty]
    private bool _passUnderbelly1IsGreen;

    [ObservableProperty]
    private bool _passUnderbelly2IsGreen;

    [ObservableProperty]
    private string _passUnderbelly1Display = string.Empty;

    [ObservableProperty]
    private string _passUnderbelly2Display = string.Empty;

    [ObservableProperty]
    private string _underbellyPassDisplay = string.Empty;

    public void Update(DecodedInGameScenario scenario)
    {
        if (!IsValidScenario(scenario.ScenarioName)) return;

        PassUnderbelly1 = scenario.PassUnderbelly1;
        PassUnderbelly2 = scenario.PassUnderbelly2;
        PassUnderbelly3 = scenario.PassUnderbelly3;
        EscapeTime = scenario.EscapeTime;

        IsUnderbellyPasswordVisible = DetermineIsUnderbellyPasswordVisible();
        PassUnderbelly1IsGreen = DeterminePassUnderbelly1IsGreen();
        PassUnderbelly2IsGreen = DeterminePassUnderbelly2IsGreen();
        PassUnderbelly1Display = CalculatePassUnderbelly1Display(scenario.GasRandom);
        PassUnderbelly2Display = CalculatePassUnderbelly2Display(scenario.GasRandom);
        UnderbellyPassDisplay = GetUnderbellyPassDisplay();
    }

    private string CalculatePassUnderbelly1Display(byte gasRandom)
    {
        return (gasRandom % 16) switch
        {
            0 => "DESK",
            1 => "MISS",
            2 => "FREE",
            3 => "JUNK",
            4 => "NEWS",
            5 => "CARD",
            6 => "DIET",
            7 => "POEM",
            8 => "BEER",
            9 => "LOCK",
            10 => "TEST",
            11 => "SOFA",
            12 => "WINE",
            13 => "TAPE",
            14 => "GOLF",
            15 => "PLAN",
            _ => $"Unrecognized PassUnderbelly1({PassUnderbelly1})"
        };
    }

    private string CalculatePassUnderbelly2Display(byte gasRandom)
    {
        return (gasRandom % 16) switch
        {
            0 => "2916",
            1 => "3719",
            2 => "0154",
            3 => "6443",
            4 => "7688",
            5 => "1812",
            6 => "5551",
            7 => "6010",
            8 => "0652",
            9 => "6234",
            10 => "0533",
            11 => "9439",
            12 => "1421",
            13 => "1127",
            14 => "7840",
            15 => "6910",
            _ => $"Unrecognized PassUnderbelly2({PassUnderbelly2})"
        };
    }

    private bool DetermineIsUnderbellyPasswordVisible()
        => EscapeTime is 0 or -1;

    private bool DeterminePassUnderbelly1IsGreen()
        => PassUnderbelly3 % 64 >= 32;

    private bool DeterminePassUnderbelly2IsGreen()
        => PassUnderbelly3 % 32 >= 16;

    private string GetUnderbellyPassDisplay()
    {
        return IsUnderbellyPasswordVisible
            ? $"{PassUnderbelly1Display}-{PassUnderbelly2Display}"
            : TimeUtility.GetTimeToString3(EscapeTime);
    }

    private static bool IsValidScenario(string scenarioName)
        => !string.IsNullOrEmpty(scenarioName)
           && scenarioName.Equals("Underbelly", StringComparison.Ordinal);
}