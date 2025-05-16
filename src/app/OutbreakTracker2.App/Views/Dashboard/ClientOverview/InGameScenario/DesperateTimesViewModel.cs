using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGameScenario;

public partial class DesperateTimesViewModel : ObservableObject
{
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

    // Used in "Desperate Times"
    [ObservableProperty]
    private byte _killedZombie;

    [ObservableProperty]
    private short _passDesperateTimes1;

    [ObservableProperty]
    private byte _passDesperateTimes2;

    [ObservableProperty]
    private byte _passDesperateTimes3;

    // Computed properties
    [ObservableProperty]
    private string _fightTimeDisplay = string.Empty;

    [ObservableProperty]
    private string _fightTime2Display = string.Empty;

    [ObservableProperty]
    private string _garageTimeDisplay = string.Empty;

    [ObservableProperty]
    private string _gasTimeDisplay = string.Empty;

    [ObservableProperty]
    private string _passDesperateTimesDisplay = string.Empty;

    [ObservableProperty]
    private IEnumerable<int> _gasRoomIdsDisplay = [];

    [ObservableProperty]
    private string _gasRoomNamesFormattedDisplay = string.Empty;

    public void Update(DecodedInGameScenario scenario)
    {
        if (!IsValidScenario(scenario.ScenarioName)) return;

        FightTime = scenario.FightTime;
        FightTime2 = scenario.FightTime2;
        GarageTime = scenario.GarageTime;
        GasTime = scenario.GasTime;
        GasFlag = scenario.GasFlag;
        KilledZombie = scenario.KilledZombie;
        PassDesperateTimes1 = scenario.PassDesperateTimes1;
        PassDesperateTimes2 = scenario.PassDesperateTimes2;
        PassDesperateTimes3 = scenario.PassDesperateTimes3;

        FightTimeDisplay = GetFightTimeDisplay();
        FightTime2Display = GetFightTime2Display();
        GarageTimeDisplay = GetGarageTimeDisplay();
        GasTimeDisplay = GetGasTimeDisplay();
        PassDesperateTimesDisplay = CalculatePassDesperateTimesDisplay(scenario.GasRandom);
        GasRoomIdsDisplay = CalculateGasRoomIdsDisplay(scenario.ScenarioName, scenario.GasRandom, scenario.GasFlag);
        GasRoomNamesFormattedDisplay = FormatGasRoomNamesDisplay(scenario.Difficulty, scenario.GasRandom, scenario.GasFlag);
    }

    private string CalculatePassDesperateTimesDisplay(int gasRandom)
    {
        return (gasRandom % 16) switch
        {
            0 => "2236",
            1 => "1587",
            2 => "2994",
            3 => "3048",
            4 => "4425",
            5 => "5170",
            6 => "6703",
            7 => "7312",
            8 => "8669",
            9 => "9851",
            10 => "0764",
            11 => "3516",
            12 => "5835",
            13 => "6249",
            14 => "7177",
            15 => "9408",
            _ => $"Unrecognized PassDesperateTimes1({PassDesperateTimes1})"
        };
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

    private static List<int> CalculateGasRoomIdsDisplay(string difficulty, int gasRandom, int gasFlag)
    {
        List<int> roomIds = [];
        (int, int GasFlag) key = (gasRandom % 2, gasFlag);

        if (RoomMapping.TryGetValue(key, out (int? HardRoomId, List<int> BaseRoomIds) mapping))
        {
            if (IsHardOrVeryHard(difficulty) && mapping.HardRoomId.HasValue)
                roomIds.Add(mapping.HardRoomId.Value);

            roomIds.AddRange(mapping.BaseRoomIds);
        }

        return roomIds;
    }

    private static string FormatGasRoomNamesDisplay(string difficulty, int gasRandom, int gasFlag)
    {
        List<int> roomIds = CalculateGasRoomIdsDisplay(difficulty, gasRandom, gasFlag);
        if (roomIds.Count is 0)
            return string.Empty;

        IEnumerable<string> roomNames = roomIds.Select(GetRoomName);
        return string.Join(Environment.NewLine, roomNames);
    }

    private static bool IsHardOrVeryHard(string difficulty)
    {
        return difficulty.Equals("hard", StringComparison.Ordinal)
               || difficulty.Equals("very hard", StringComparison.Ordinal);
    }

    private static string GetRoomName(int roomId)
    {
        return EnumUtility.TryParseByValueOrMember("Desperate times", out Scenario scenarioEnum)
            ? scenarioEnum.GetRoomName(roomId)
            : $"Room Id: {roomId}";
    }

    private static bool IsValidScenario(string scenarioName)
    {
        return !string.IsNullOrEmpty(scenarioName)
               && scenarioName.Equals("desperate times", StringComparison.Ordinal);
    }

    private string GetFightTimeDisplay() => TimeUtility.GetTimeToString3(FightTime);
    private string GetFightTime2Display() => TimeUtility.GetTimeToString3(FightTime2);
    private string GetGarageTimeDisplay() => TimeUtility.GetTimeToString3(GarageTime);
    private string GetGasTimeDisplay() => TimeUtility.GetTimeToString3(GasTime);
}