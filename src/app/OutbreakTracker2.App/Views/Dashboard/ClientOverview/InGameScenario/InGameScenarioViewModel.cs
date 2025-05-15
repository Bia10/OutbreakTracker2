using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.App.Services.Data;
using OutbreakTracker2.App.Services.Dispatcher;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Utility;
using R3;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGameScenario;

// TODO: needs a rewrite
public partial class InGameScenarioViewModel : ObservableObject
{
    private readonly ILogger<InGameScenarioViewModel> _logger;
    private readonly IDataManager _dataManager;

    public InGameScenarioViewModel(
        IDataManager dataManager,
        ILogger<InGameScenarioViewModel> logger,
        IDispatcherService dispatcherService)
    {
        _dataManager = dataManager;
        _logger = logger;

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
        Cleared = scenario.Cleared;
        WildThingsTime = scenario.WildThingsTime;
        EscapeTime = scenario.EscapeTime;
        FightTime = scenario.FightTime;
        FightTime2 = scenario.FightTime2;
        GarageTime = scenario.GarageTime;
        GasTime = scenario.GasTime;
        GasFlag = scenario.GasFlag;
        GasRandom = scenario.GasRandom;
        ItemRandom = scenario.ItemRandom;
        ItemRandom2 = scenario.ItemRandom2;
        PuzzleRandom = scenario.PuzzleRandom;
        Coin = scenario.Coin;
        KilledZombie = scenario.KilledZombie;
        PlayerCount = scenario.PlayerCount;
        PassDesperateTimes1 = scenario.PassDesperateTimes1;
        PassWildThings = scenario.PassWildThings;
        PassDesperateTimes2 = scenario.PassDesperateTimes2;
        PassDesperateTimes3 = scenario.PassDesperateTimes3;
        Pass1 = scenario.Pass1;
        Pass2 = scenario.Pass2;
        Pass3 = scenario.Pass3;
        PassUnderbelly1 = scenario.PassUnderbelly1;
        PassUnderbelly2 = scenario.PassUnderbelly2;
        PassUnderbelly3 = scenario.PassUnderbelly3;
        Pass4 = scenario.Pass4;
        Pass5 = scenario.Pass5;
        Pass6 = scenario.Pass6;

        OnPropertyChanged(nameof(ClearedDisplay));
        OnPropertyChanged(nameof(PlayerCountDisplay));
        OnPropertyChanged(nameof(GameTimeDisplay));
        OnPropertyChanged(nameof(WildThingsTimeDisplay));
        OnPropertyChanged(nameof(EscapeTimeDisplay));
        OnPropertyChanged(nameof(FightTimeDisplay));
        OnPropertyChanged(nameof(FightTime2Display));
        OnPropertyChanged(nameof(GarageTimeDisplay));
        OnPropertyChanged(nameof(GasTimeDisplay));
        OnPropertyChanged(nameof(PassBelowFreezingPointDisplay));
        OnPropertyChanged(nameof(Pass2BelowFreezingPointDisplay));
        OnPropertyChanged(nameof(PassHiveDisplay));
        OnPropertyChanged(nameof(HellfirePassDisplay));
        OnPropertyChanged(nameof(HellfireMapDisplay));
        OnPropertyChanged(nameof(HellfirePowerDisplay));
        OnPropertyChanged(nameof(DecisionsDecisionsPassDisplay));
        OnPropertyChanged(nameof(ClockTimeDisplay));
        OnPropertyChanged(nameof(GasRandomOrderDisplay));
        OnPropertyChanged(nameof(PassWildThingsDisplay));
        OnPropertyChanged(nameof(PassDesperateTimesDisplay));
        OnPropertyChanged(nameof(PassUnderbelly1Display));
        OnPropertyChanged(nameof(PassUnderbelly2Display));
        OnPropertyChanged(nameof(GasRoomIdsDisplay));
        OnPropertyChanged(nameof(GasRoomNamesFormattedDisplay));
        OnPropertyChanged(nameof(BelowFreezingPointPasswordDisplay));
        OnPropertyChanged(nameof(HellfireDisplay));
        OnPropertyChanged(nameof(DecisionsDecisionsDisplay));
        OnPropertyChanged(nameof(IsUnderbellyPasswordVisible));
        OnPropertyChanged(nameof(PassUnderbelly1IsGreen));
        OnPropertyChanged(nameof(PassUnderbelly2IsGreen));
        OnPropertyChanged(nameof(UnderbellyDisplay));
        OnPropertyChanged(nameof(EndOfRoadDisplay));
    }

    [ObservableProperty]
    private byte _currentFile;

    [ObservableProperty]
    private string _scenarioName = string.Empty;

    [ObservableProperty]
    private int _frameCounter;

    [ObservableProperty]
    private byte _cleared;

    [ObservableProperty]
    private short _wildThingsTime;

    // Used in "Underbelly" and "Flashback"
    [ObservableProperty]
    private short _escapeTime;

    // Used in "Desperate Times"
    [ObservableProperty]
    private int _fightTime;

    // Used in "Desperate Times"
    [ObservableProperty]
    private short _fightTime2;

    // Used in "Desperate Times"
    [ObservableProperty]
    private int _garageTime;

    // Used in "Desperate Times"
    [ObservableProperty]
    private int _gasTime;

    // Likely a bitmask
    [ObservableProperty]
    private int _gasFlag;

    // A bit interesting that this is used in decoding various passwords and other apparently unrelated things
    // feels like a bit more generic random seed then just for the gas
    [ObservableProperty]
    private byte _gasRandom;

    [ObservableProperty]
    private byte _itemRandom;

    [ObservableProperty]
    private byte _itemRandom2;

    // Used for Hellfire power
    [ObservableProperty]
    private byte _puzzleRandom;

    // Used in "Wild Things"
    [ObservableProperty]
    private byte _coin;

    // Used in "Desperate Times"
    [ObservableProperty]
    private byte _killedZombie;

    [ObservableProperty]
    private byte _playerCount;

    [ObservableProperty]
    private short _passDesperateTimes1;

    // Used for Wild Things
    [ObservableProperty]
    private byte _passWildThings;

    [ObservableProperty]
    private byte _passDesperateTimes2;

    [ObservableProperty]
    private byte _passDesperateTimes3;

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

    [ObservableProperty]
    private string _difficulty = string.Empty;

    public string ClearedDisplay => GetClearedDisplay();
    public string PlayerCountDisplay => GetPlayerCountDisplay();
    public string GameTimeDisplay => GetGameTime();
    public string WildThingsTimeDisplay => GetWildThingsTimeDisplay();
    public string EscapeTimeDisplay => GetEscapeTimeDisplay();
    public string FightTimeDisplay => GetFightTimeDisplay();
    public string FightTime2Display => GetFightTime2Display();
    public string GarageTimeDisplay => GetGarageTimeDisplay();
    public string GasTimeDisplay => GetGasTimeDisplay();
    public string PassBelowFreezingPointDisplay => CalculatePassBelowFreezingPointDisplay();
    public string Pass2BelowFreezingPointDisplay => CalculatePass2BelowFreezingPointDisplay();
    public string PassHiveDisplay => CalculatePassHiveDisplay();
    public string HellfirePassDisplay => CalculateHellfirePassDisplay();
    public string HellfireMapDisplay => CalculateHellfireMapDisplay();
    public string HellfirePowerDisplay => CalculateHellfirePowerDisplay();
    public string DecisionsDecisionsPassDisplay => CalculateDecisionsDecisionsPassDisplay();
    public string ClockTimeDisplay => CalculateClockTimeDisplay();
    public int GasRandomOrderDisplay => CalculateGasRandomOrderDisplay();
    public string PassWildThingsDisplay => CalculatePassWildThingsDisplay();
    public string PassDesperateTimesDisplay => CalculatePassDesperateTimesDisplay();
    public string PassUnderbelly1Display => CalculatePassUnderbelly1Display();
    public string PassUnderbelly2Display => CalculatePassUnderbelly2Display();
    public IEnumerable<int>? GasRoomIdsDisplay => CalculateGasRoomIdsDisplay();
    public string GasRoomNamesFormattedDisplay => FormatGasRoomNamesDisplay();
    public string BelowFreezingPointPasswordDisplay => GetBelowFreezingPointPasswordDisplay();
    public string HellfireDisplay => GetHellfireDisplay();
    public string DecisionsDecisionsDisplay => GetDecisionsDecisionsDisplay();
    public bool IsUnderbellyPasswordVisible => DetermineIsUnderbellyPasswordVisible();
    public bool PassUnderbelly1IsGreen => DeterminePassUnderbelly1IsGreen();
    public bool PassUnderbelly2IsGreen => DeterminePassUnderbelly2IsGreen();
    public string UnderbellyDisplay => GetUnderbellyDisplay();
    public string EndOfRoadDisplay => GetEndOfRoadDisplay();

    private string GetClearedDisplay() => Cleared > 0 ? "Yes" : "No";
    private string GetPlayerCountDisplay() => $"{PlayerCount} Players";
    private string GetGameTime() => TimeUtility.GetTimeFromFrames(FrameCounter);
    private string GetWildThingsTimeDisplay() => TimeUtility.GetTimeToString3(WildThingsTime);
    private string GetEscapeTimeDisplay() => TimeUtility.GetTimeToString3(EscapeTime);
    private string GetFightTimeDisplay() => TimeUtility.GetTimeToString3(FightTime);
    private string GetFightTime2Display() => TimeUtility.GetTimeToString3(FightTime2);
    private string GetGarageTimeDisplay() => TimeUtility.GetTimeToString3(GarageTime);
    private string GetGasTimeDisplay() => TimeUtility.GetTimeToString3(GasTime);

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

    private string CalculatePassDesperateTimesDisplay()
    {
        switch (GasRandom % 16)
        {
            case 0: return "2236";
            case 1: return "1587";
            case 2: return "2994";
            case 3: return "3048";
            case 4: return "4425";
            case 5: return "5170";
            case 6: return "6703";
            case 7: return "7312";
            case 8: return "8669";
            case 9: return "9851";
            case 10: return "0764";
            case 11: return "3516";
            case 12: return "5835";
            case 13: return "6249";
            case 14: return "7177";
            case 15: return "9408";

            default:
                _logger.LogDebug("Unrecognized PassDesperateTimes1 value: {PassDesperateTimes}", PassDesperateTimes1);
                return $"Unrecognized PassDesperateTimes1({PassDesperateTimes1})";
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
        switch ((GasRandom % 16))
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

    private bool IsHardOrVeryHard()
    {
        return Difficulty.Equals("hard", StringComparison.Ordinal)
               || Difficulty.Equals("very hard", StringComparison.Ordinal);
    }

    private static readonly Dictionary<(int Modulo, int Flag), (int? HardRoomId, List<int> BaseRoomIds)> RoomMapping =
        new()
        {
            // --- GasRandom % 2 == 0 Cases ---
            { (0, 1), (4, [14, 20]) },
            { (0, 2), (7, [10, 12]) },
            { (0, 4), (9, [13, 27]) },
            { (0, 8), (5, [7, 21]) },
            { (0, 16), (4, [10, 11]) },
            { (0, 32), (5, [15, 16]) },
            { (0, 64), (4, [11, 13]) },
            { (0, 128), (14, [15, 21]) },
            { (0, 256), (11, [20, 27]) },
            { (0, 512), (5, [9, 16]) },

            // --- GasRandom % 2 == 1 Cases ---
            { (1, 1), (7, [10, 16]) },
            { (1, 2), (4, [14, 27]) },
            { (1, 4), (7, [16, 20]) },
            { (1, 8), (9, [13, 21]) },
            { (1, 16), (10, [12, 15]) },
            { (1, 32), (5, [16, 21]) },
            { (1, 64), (5, [11, 27]) },
            { (1, 128), (4, [7, 20]) },
            { (1, 256), (10, [12, 13]) },
            { (1, 512), (7, [15, 21]) },
        };

    private List<int>? CalculateGasRoomIdsDisplay()
    {
        if (!ScenarioName.Equals("desperate times", StringComparison.Ordinal))
            return null;

        List<int> roomIds = [];
        (int, int GasFlag) key = (GasRandom % 2, GasFlag);

        if (RoomMapping.TryGetValue(key, out (int? HardRoomId, List<int> BaseRoomIds) mapping))
        {
            if (IsHardOrVeryHard() && mapping.HardRoomId.HasValue)
                roomIds.Add(mapping.HardRoomId.Value);

            roomIds.AddRange(mapping.BaseRoomIds);
        }

        return roomIds;
    }

    private string FormatGasRoomNamesDisplay()
    {
        IEnumerable<int> roomIds = (CalculateGasRoomIdsDisplay() ?? []).ToArray();
        if (!roomIds.Any())
            return string.Empty;

        IEnumerable<string> roomNames = roomIds.Select(id => GetRoomName((short)id));
        return string.Join(Environment.NewLine, roomNames);
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

    private string GetRoomName(short roomId)
    {
        string curScenarioName = _dataManager.InGameScenario.ScenarioName;
        if (!string.IsNullOrEmpty(curScenarioName) && EnumUtility.TryParseByValueOrMember(curScenarioName, out Outbreak.Enums.InGameScenario scenarioEnum))
            return scenarioEnum.GetRoomName(roomId);

        return $"Room ID: {roomId}";
    }
}