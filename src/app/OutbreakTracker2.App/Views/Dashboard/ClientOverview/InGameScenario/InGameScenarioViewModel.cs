using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.App.Services.Data;
using OutbreakTracker2.App.Services.Dispatcher;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Utility;
using R3;
using System;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGameScenario;

// TODO: needs a rewrite
public partial class InGameScenarioViewModel : ObservableObject
{
    private readonly ILogger<InGameScenarioViewModel> _logger;

    [ObservableProperty]
    private byte _currentFile;

    [ObservableProperty]
    private string _scenarioName = string.Empty;

    [ObservableProperty]
    private int _frameCounter;

    [ObservableProperty]
    private string _gameTimeDisplay;

    // TODO: unknown statuses
    // 0 = scenario not in progress
    // 1 = loading after cinematic sequence
    // 2 = ingame scenario in progress
    // 7 = loading scenario
    // 12 = finished (died)
    // 13 = after game stats
    // 14 = intro scenario cinematic sequence
    // 15 = after save to memory card
    [ObservableProperty]
    private byte _status;

    [ObservableProperty]
    private string _isCleared = string.Empty;

    [ObservableProperty]
    private string _difficulty = string.Empty;

    [ObservableProperty]
    private byte _playerCount;

    [ObservableProperty]
    private string _playerCountDisplay;

    // Used in Underbelly, Flashback
    [ObservableProperty]
    private byte _itemRandom;

    // Used in Underbelly, Flashback
    [ObservableProperty]
    private byte _itemRandom2;

    // Used in Underbelly, Flashback, Hellfire
    [ObservableProperty]
    private byte _puzzleRandom;

    // Used in Desperate Times, however this seems more like generic random seed then scenario specific
    // used in calculations unrelated to Desperate Times
    [ObservableProperty]
    private byte _gasRandom;

    [ObservableProperty]
    private int _gasRandomOrderDisplay;

    [ObservableProperty]
    private DesperateTimesViewModel _desperateTimesVm;

    public InGameScenarioViewModel(
        ILogger<InGameScenarioViewModel> logger,
        IDataManager dataManager,
        IDispatcherService dispatcherService)
    {
        _logger = logger;
        _desperateTimesVm = new DesperateTimesViewModel();

        dataManager.InGameScenarioObservable
            .ObserveOnThreadPool()
            .SubscribeAwait(async (inGameScenario, cancellationToken) =>
            {
                _logger.LogTrace("Processing inGame scenario data on thread pool");
                try
                {
                    await dispatcherService.InvokeOnUIAsync(() =>
                    {
                        _logger.LogTrace("Updating InGameScenarioViewModel properties on UI thread");
                        Update(inGameScenario);
                    }, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("InGame scenario data processing cancelled");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during inGame scenario data processing cycle");
                }
            }, AwaitOperation.Drop);
    }

    public void Update(DecodedInGameScenario scenario)
    {
        CurrentFile = scenario.CurrentFile;
        ScenarioName = scenario.ScenarioName;
        FrameCounter = scenario.FrameCounter;
        Difficulty = scenario.Difficulty;
        Status = scenario.Status;
        PlayerCount = scenario.PlayerCount;
        ItemRandom = scenario.ItemRandom;
        ItemRandom2 = scenario.ItemRandom2;
        PuzzleRandom = scenario.PuzzleRandom;
        GasRandom = scenario.GasRandom;

        PlayerCountDisplay = GetPlayerCountDisplay();
        GameTimeDisplay = GetGameTime();
        GasRandomOrderDisplay = CalculateGasRandomOrderDisplay();
        IsCleared = GetClearedDisplay();

        //EscapeTime = scenario.EscapeTime;
        //WildThingsTime = scenario.WildThingsTime;
        //Coin = scenario.Coin;
        //PassWildThings = scenario.PassWildThings;
        //Pass1 = scenario.Pass1;
        //Pass2 = scenario.Pass2;
        //Pass3 = scenario.Pass3;
        //PassUnderbelly1 = scenario.PassUnderbelly1;
        //PassUnderbelly2 = scenario.PassUnderbelly2;
        //PassUnderbelly3 = scenario.PassUnderbelly3;
        //Pass4 = scenario.Pass4;
        //Pass5 = scenario.Pass5;
        //Pass6 = scenario.Pass6;

        //OnPropertyChanged(nameof(EscapeTimeDisplay));
        //OnPropertyChanged(nameof(WildThingsTimeDisplay));
        //OnPropertyChanged(nameof(PassBelowFreezingPointDisplay));
        //OnPropertyChanged(nameof(Pass2BelowFreezingPointDisplay));
        //OnPropertyChanged(nameof(PassHiveDisplay));
        //OnPropertyChanged(nameof(HellfirePassDisplay));
        //OnPropertyChanged(nameof(HellfireMapDisplay));
        //OnPropertyChanged(nameof(HellfirePowerDisplay));
        //OnPropertyChanged(nameof(DecisionsDecisionsPassDisplay));
        //OnPropertyChanged(nameof(ClockTimeDisplay));
        //OnPropertyChanged(nameof(GasRandomOrderDisplay));
        //OnPropertyChanged(nameof(PassWildThingsDisplay));
        //OnPropertyChanged(nameof(PassUnderbelly1Display));
        //OnPropertyChanged(nameof(PassUnderbelly2Display));
        //OnPropertyChanged(nameof(BelowFreezingPointPasswordDisplay));
        //OnPropertyChanged(nameof(HellfireDisplay));
        //OnPropertyChanged(nameof(DecisionsDecisionsDisplay));
        //OnPropertyChanged(nameof(IsUnderbellyPasswordVisible));
        //OnPropertyChanged(nameof(PassUnderbelly1IsGreen));
        //OnPropertyChanged(nameof(PassUnderbelly2IsGreen));
        //OnPropertyChanged(nameof(UnderbellyDisplay));
        //OnPropertyChanged(nameof(EndOfRoadDisplay));
    }

    // Used in "Underbelly" and "Flashback"
    [ObservableProperty]
    private short _escapeTime;

    // Used in "Wild Things"
    [ObservableProperty]
    private byte _coin;

    // Used in "Wild Things"
    [ObservableProperty]
    private short _wildThingsTime;

    // Used for Wild Things
    [ObservableProperty]
    private byte _passWildThings;

    // Used for Below Freezing Point and Hive
    [ObservableProperty]
    private byte _pass1;

    // Used for Below Freezing Point
    [ObservableProperty]
    private byte _pass2;

    // Used in Hellfire and Decisions,decisions (with pass6)
    [ObservableProperty]
    private byte _pass3;

    [ObservableProperty]
    private short _passUnderbelly1;

    [ObservableProperty]
    private byte _passUnderbelly2;

    [ObservableProperty]
    private byte _passUnderbelly3;

    // Used for Hellfire, also displayed raw in "End of the Road"
    [ObservableProperty]
    private short _pass4;

    // Use unclear ??
    [ObservableProperty]
    private byte _pass5;

    // Used for Decisions,decisions (with pass3)
    [ObservableProperty]
    private byte _pass6;

    public string EscapeTimeDisplay => GetEscapeTimeDisplay();
    public string WildThingsTimeDisplay => GetWildThingsTimeDisplay();
    public string PassBelowFreezingPointDisplay => CalculatePassBelowFreezingPointDisplay();
    public string Pass2BelowFreezingPointDisplay => CalculatePass2BelowFreezingPointDisplay();
    public string PassHiveDisplay => CalculatePassHiveDisplay();
    public string HellfirePassDisplay => CalculateHellfirePassDisplay();
    public string HellfireMapDisplay => CalculateHellfireMapDisplay();
    public string HellfirePowerDisplay => CalculateHellfirePowerDisplay();
    public string DecisionsDecisionsPassDisplay => CalculateDecisionsDecisionsPassDisplay();
    public string ClockTimeDisplay => CalculateClockTimeDisplay();
    public string PassWildThingsDisplay => CalculatePassWildThingsDisplay();
    public string PassUnderbelly1Display => CalculatePassUnderbelly1Display();
    public string PassUnderbelly2Display => CalculatePassUnderbelly2Display();
    public string BelowFreezingPointPasswordDisplay => GetBelowFreezingPointPasswordDisplay();
    public string HellfireDisplay => GetHellfireDisplay();
    public string DecisionsDecisionsDisplay => GetDecisionsDecisionsDisplay();
    public bool IsUnderbellyPasswordVisible => DetermineIsUnderbellyPasswordVisible();
    public bool PassUnderbelly1IsGreen => DeterminePassUnderbelly1IsGreen();
    public bool PassUnderbelly2IsGreen => DeterminePassUnderbelly2IsGreen();
    public string UnderbellyDisplay => GetUnderbellyDisplay();
    public string EndOfRoadDisplay => GetEndOfRoadDisplay();

    private string GetClearedDisplay() => Status is 12 or 13 or 15 ? "Yes" : "No";
    private string GetPlayerCountDisplay() => $"{PlayerCount} Players";
    private string GetGameTime() => TimeUtility.GetTimeFromFrames(FrameCounter);
    private string GetWildThingsTimeDisplay() => TimeUtility.GetTimeToString3(WildThingsTime);
    private string GetEscapeTimeDisplay() => TimeUtility.GetTimeToString3(EscapeTime);

    private string CalculatePassBelowFreezingPointDisplay()
    {
        switch (Pass1)
        {
            case > 0x00 and <= 0x1F or >= 0x80 and <= 0x9F: return "0634";
            case >= 0x20 and <= 0x3F or >= 0xA0 and <= 0xBF: return "4509";
            case >= 0x40 and <= 0x7F or >= 0xC0 and < 0xFF: return "9741";

            default:
                _logger.LogDebug("Unrecognized BFP Pass1 value: {Pass1}", Pass1);
                return $"Unrecognized BFP Pass1({Pass1})";
        }
    }

    private string CalculatePass2BelowFreezingPointDisplay()
    {
        switch (Pass2)
        {
            case 0x20: return "A375-B482";
            case 0x40: return "J126-D580";
            case 0x80: return "C582-A194";

            default:
                _logger.LogDebug("Unrecognized BFP Pass2 value: {Pass2}", Pass2);
                return $"Unrecognized BFP Pass2({Pass2})";
        }
    }

    private string CalculatePassHiveDisplay()
    {
        switch (Pass1)
        {
            case > 0x00 and <= 0x1F or >= 0x80 and <= 0x9F: return "3555-0930";
            case >= 0x20 and <= 0x3F or >= 0x60 and <= 0x7F or >= 0xA0 and <= 0xBF or >= 0xE0 and < 0xFF: return "5315-0930";
            case >= 0x40 and <= 0x5F or >= 0xC0 and < 0xDF: return "8211-0930";

            default:
                _logger.LogDebug("Unrecognized Hive Pass1 value: {Pass1}", Pass1);
                return $"Unrecognized Hive Pass1({Pass1})";
        }
    }

    private string CalculateHellfirePassDisplay()
    {
        switch (Pass3)
        {
            case > 0x0 and <= 0x1: return "0721-DCH";
            case >= 0x2 and <= 0x3: return "2287-JIA";
            case >= 0x4 and <= 0x7: return "6354-BAE";
            case >= 0x8 and <= 0xF: return "5128-GGF";

            default:
                _logger.LogDebug("Unrecognized Hellfire Pass3 value: {Pass3}", Pass3);
                return $"Unrecognized Hellfire Pass3({Pass3})";
        }
    }

    private string CalculateHellfireMapDisplay()
    {
        switch (Pass4)
        {
            case 0x4000: return "1234";
            case 0x4020: return "234";
            case 0x4040: return "134";
            case 0x4060: return "34";
            case 0x4080: return "124";
            case 0x40A0: return "24";
            case 0x40C0: return "14";
            case 0x40E0: return "4";
            case 0x4100: return "123";
            case 0x4120: return "23";
            case 0x4140: return "13";
            case 0x4160: return "3";
            case 0x4180: return "12";
            case 0x41A0: return "2";
            case 0x41C0: return "1";

            default:
                _logger.LogDebug("Unrecognized Hellfire Pass4 value: {Pass4}", Pass4);
                return $"Unrecognized Hellfire Pass4({Pass4})";
        }
    }

    private string CalculateHellfirePowerDisplay()
        => PuzzleRandom % 2 == 0 ? "1" : "2";

    private string CalculateDecisionsDecisionsPassDisplay()
    {
        if (Pass3 is > 0x00 and < 0x40 && Pass6 % 2 == 0x0)
            return "4284";

        switch (Pass3)
        {
            case >= 0x40 and < 0x80: return "4161";
            case >= 0x80 when Pass6 == 0: return "4032";
        }

        if (Pass3 is > 0x00 and < 0x40 && Pass6 % 2 == 0x1)
            return "4927";

        _logger.LogDebug("Unrecognized Decisions,decisions Pass3 value: {Pass3}", Pass3);
        return $"Unrecognized Decisions,decisions Pass3({Pass3})";
    }

    private string CalculateClockTimeDisplay()
    {
        return Difficulty.ToLowerInvariant() switch
        {
            "easy" => "03:25",
            "normal" => "10:05",
            "hard" => "07:40",
            "very hard" => "02:50",
            _ => "N/A",
        };
    }

    private int CalculateGasRandomOrderDisplay()
    {
        switch (GasRandom)
        {
            case > 0 and < 240: return (GasRandom / 10) + 1;
            case >= 240 and < 255: return 25;
            default:
                _logger.LogDebug("Unrecognized GasRandom value: {GasRandom}", GasRandom);
                return -1;
        }
    }

    private string CalculatePassWildThingsDisplay()
    {
        switch (GasRandom % 16)
        {
            case 0: return "39DJ";
            case 1: return "LV4U";
            case 2: return "EXP2";
            case 3: return "E67C";
            case 4: return "6SR2";
            case 5: return "Q898";
            case 6: return "44V7";
            case 7: return "K3G6";
            case 8: return "SW4D";
            case 9: return "FM54";
            case 10: return "5TF3";
            case 11: return "4NZH";
            case 12: return "B37B";
            case 13: return "LYNX";
            case 14: return "9AAA";
            case 15: return "YTY7";

            default:
                _logger.LogDebug("Unrecognized PassWildThings value: {PassWildThings}", PassWildThings);
                return $"Unrecognized PassWildThings({PassWildThings}) ";
        }
    }

    private string CalculatePassUnderbelly1Display()
    {
        switch (GasRandom % 16)
        {
            case 0: return "DESK";
            case 1: return "MISS";
            case 2: return "FREE";
            case 3: return "JUNK";
            case 4: return "NEWS";
            case 5: return "CARD";
            case 6: return "DIET";
            case 7: return "POEM";
            case 8: return "BEER";
            case 9: return "LOCK";
            case 10: return "TEST";
            case 11: return "SOFA";
            case 12: return "WINE";
            case 13: return "TAPE";
            case 14: return "GOLF";
            case 15: return "PLAN";

            default:
                _logger.LogDebug("Unrecognized PassUnderbelly1 value: {PassUnderbelly1}", PassUnderbelly1);
                return $"Unrecognized PassUnderbelly1({PassUnderbelly1})";
        }
    }

    private string CalculatePassUnderbelly2Display()
    {
        switch (GasRandom % 16)
        {
            case 0: return "2916";
            case 1: return "3719";
            case 2: return "0154";
            case 3: return "6443";
            case 4: return "7688";
            case 5: return "1812";
            case 6: return "5551";
            case 7: return "6010";
            case 8: return "0652";
            case 9: return "6234";
            case 10: return "0533";
            case 11: return "9439";
            case 12: return "1421";
            case 13: return "1127";
            case 14: return "7840";
            case 15: return "6910";

            default:
                _logger.LogDebug("Unrecognized PassUnderbelly2 value: {PassUnderbelly2}", PassUnderbelly2);
                return $"Unrecognized PassUnderbelly2({PassUnderbelly2})";
        }
    }

    private string GetBelowFreezingPointPasswordDisplay()
    {
        return !ScenarioName.Equals("below freezing point", StringComparison.Ordinal)
            ? string.Empty
            : $"{CalculatePassBelowFreezingPointDisplay()}-{CalculatePass2BelowFreezingPointDisplay()}";
    }

    private string GetHellfireDisplay()
    {
        return !ScenarioName.Equals("hellfire", StringComparison.Ordinal)
            ? string.Empty
            : $"{CalculateHellfirePassDisplay()}-{CalculateHellfireMapDisplay()}-{CalculateHellfirePowerDisplay()}";
    }

    private string GetDecisionsDecisionsDisplay()
    {
        return !ScenarioName.Equals("decisions,decisions", StringComparison.Ordinal)
            ? string.Empty
            : $"{CalculateDecisionsDecisionsPassDisplay()}-{CalculateClockTimeDisplay()}";
    }

    private bool DetermineIsUnderbellyPasswordVisible()
        => EscapeTime is 0 or -1;

    private bool DeterminePassUnderbelly1IsGreen()
        => PassUnderbelly3 % 64 >= 32;

    private bool DeterminePassUnderbelly2IsGreen()
        => PassUnderbelly3 % 32 >= 16;

    private string GetUnderbellyDisplay()
    {
        if (!ScenarioName.Equals("underbelly", StringComparison.Ordinal))
            return string.Empty;

        return DetermineIsUnderbellyPasswordVisible()
            ? $"{CalculatePassUnderbelly1Display()} {CalculatePassUnderbelly2Display()}"
            : GetEscapeTimeDisplay();
    }

    private string GetEndOfRoadDisplay()
    {
        return !ScenarioName.Equals("end of the road", StringComparison.Ordinal)
            ? string.Empty
            : Pass4.ToString();
    }
}