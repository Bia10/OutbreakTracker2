namespace OutbreakTracker2.UnitTests;

public class GasRoomIdsCalculationTests
{
    private static bool IsHardOrVeryHard(bool isHardOrVeryHardResult)
    {
        return isHardOrVeryHardResult;
    }

    private static List<int>? CalculateGasRoomIdsDisplay_Original(
        string scenarioName,
        int gasRandom,
        int gasFlag,
        bool isHardOrVeryHardResult
    )
    {
        if (!scenarioName.Equals("desperate times", StringComparison.Ordinal))
            return null;

        bool isHardOrVeryHard = IsHardOrVeryHard(isHardOrVeryHardResult);
        List<int> roomIds = [];

        switch (gasRandom % 2)
        {
            case 0:
                switch (gasFlag)
                {
                    case 1:
                    {
                        if (isHardOrVeryHard)
                            roomIds.Add(4);
                        roomIds.Add(14);
                        roomIds.Add(20);
                        break;
                    }
                    case 2:
                    {
                        if (isHardOrVeryHard)
                            roomIds.Add(7);
                        roomIds.Add(10);
                        roomIds.Add(12);
                        break;
                    }
                    case 4:
                    {
                        if (isHardOrVeryHard)
                            roomIds.Add(9);
                        roomIds.Add(13);
                        roomIds.Add(27);
                        break;
                    }
                    case 8:
                    {
                        if (isHardOrVeryHard)
                            roomIds.Add(5);
                        roomIds.Add(7);
                        roomIds.Add(21);
                        break;
                    }
                    case 16:
                    {
                        if (isHardOrVeryHard)
                            roomIds.Add(4);
                        roomIds.Add(10);
                        roomIds.Add(11);
                        break;
                    }
                    case 32:
                    {
                        if (isHardOrVeryHard)
                            roomIds.Add(5);
                        roomIds.Add(15);
                        roomIds.Add(16);
                        break;
                    }
                    case 64:
                    {
                        if (isHardOrVeryHard)
                            roomIds.Add(4);
                        roomIds.Add(11);
                        roomIds.Add(13);
                        break;
                    }
                    case 128:
                    {
                        if (isHardOrVeryHard)
                            roomIds.Add(14);
                        roomIds.Add(15);
                        roomIds.Add(21);
                        break;
                    }
                    case 256:
                    {
                        if (isHardOrVeryHard)
                            roomIds.Add(11);
                        roomIds.Add(20);
                        roomIds.Add(27);
                        break;
                    }
                    case 512:
                    {
                        if (isHardOrVeryHard)
                            roomIds.Add(5);
                        roomIds.Add(9);
                        roomIds.Add(16);
                        break;
                    }
                }

                break; // break for GasRandom % 2 == 0
            case 1:
                switch (gasFlag)
                {
                    case 1:
                    {
                        if (isHardOrVeryHard)
                            roomIds.Add(7);
                        roomIds.Add(10);
                        roomIds.Add(16);
                        break;
                    }
                    case 2:
                    {
                        if (isHardOrVeryHard)
                            roomIds.Add(4);
                        roomIds.Add(14);
                        roomIds.Add(27);
                        break;
                    }
                    case 4:
                    {
                        if (isHardOrVeryHard)
                            roomIds.Add(7);
                        roomIds.Add(16);
                        roomIds.Add(20);
                        break;
                    }
                    case 8:
                    {
                        if (isHardOrVeryHard)
                            roomIds.Add(9);
                        roomIds.Add(13);
                        roomIds.Add(21);
                        break;
                    }
                    case 16:
                    {
                        if (isHardOrVeryHard)
                            roomIds.Add(10);
                        roomIds.Add(12);
                        roomIds.Add(15);
                        break;
                    }
                    case 32:
                    {
                        if (isHardOrVeryHard)
                            roomIds.Add(5);
                        roomIds.Add(16);
                        roomIds.Add(21);
                        break;
                    }
                    case 64:
                    {
                        if (isHardOrVeryHard)
                            roomIds.Add(5);
                        roomIds.Add(11);
                        roomIds.Add(27);
                        break;
                    }
                    case 128:
                    {
                        if (isHardOrVeryHard)
                            roomIds.Add(4);
                        roomIds.Add(7);
                        roomIds.Add(20);
                        break;
                    }
                    case 256:
                    {
                        if (isHardOrVeryHard)
                            roomIds.Add(10);
                        roomIds.Add(12);
                        roomIds.Add(13);
                        break;
                    }
                    case 512:
                    {
                        if (isHardOrVeryHard)
                            roomIds.Add(7);
                        roomIds.Add(15);
                        roomIds.Add(21);
                        break;
                    }
                }

                break; // break for GasRandom % 2 == 1
        }

        return roomIds;
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

    private static List<int>? CalculateGasRoomIdsDisplay_Refactored(
        string scenarioName,
        int gasRandom,
        int gasFlag,
        bool isHardOrVeryHardResult
    )
    {
        if (!scenarioName.Equals("desperate times", StringComparison.Ordinal))
            return null;

        List<int> roomIds = [];
        (int, int GasFlag) key = (gasRandom % 2, gasFlag);

        if (RoomMapping.TryGetValue(key, out (int? HardRoomId, List<int> BaseRoomIds) mapping))
        {
            if (IsHardOrVeryHard(isHardOrVeryHardResult) && mapping.HardRoomId.HasValue)
                roomIds.Add(mapping.HardRoomId.Value);

            roomIds.AddRange(mapping.BaseRoomIds);
        }

        return roomIds;
    }

    [Test]
    [MethodDataSource(nameof(GetGasRoomIdsTestCases))]
    public async Task OriginalAndRefactoredRoomIdsMethodsAreEquivalent(
        string scenario,
        int gasRandom,
        int gasFlag,
        bool isHard
    )
    {
        List<int>? originalResult = CalculateGasRoomIdsDisplay_Original(scenario, gasRandom, gasFlag, isHard);
        List<int>? refactoredResult = CalculateGasRoomIdsDisplay_Refactored(scenario, gasRandom, gasFlag, isHard);

        await Assert.That(refactoredResult is null).IsEqualTo(originalResult is null);

        if (originalResult is not null)
        {
            await Assert.That(refactoredResult).IsNotNull();
            await Assert.That(refactoredResult!.SequenceEqual(originalResult)).IsTrue();
        }
    }

    public static IEnumerable<(string Scenario, int GasRandom, int GasFlag, bool IsHard)> GetGasRoomIdsTestCases()
    {
        // Case 1: ScenarioName is NOT "desperate times" — should return null
        yield return ("other scenario", 0, 0, false);
        yield return ("another one", 50, 100, true);

        // Case 2: ScenarioName IS "desperate times"
        const string desperateScenario = "desperate times";
        int[] definedGasFlags = [1, 2, 4, 8, 16, 32, 64, 128, 256, 512];
        int[] undefinedGasFlags = [0, 3, 5, 1000, -1];

        foreach (int flag in definedGasFlags)
        {
            yield return (desperateScenario, 0, flag, false);
            yield return (desperateScenario, 0, flag, true);
            yield return (desperateScenario, 1, flag, false);
            yield return (desperateScenario, 1, flag, true);
        }

        foreach (int flag in undefinedGasFlags)
        {
            yield return (desperateScenario, 0, flag, false);
            yield return (desperateScenario, 0, flag, true);
            yield return (desperateScenario, 1, flag, false);
            yield return (desperateScenario, 1, flag, true);
        }

        yield return (desperateScenario, 10, 1, false);
        yield return (desperateScenario, 10, 1, true);
        yield return (desperateScenario, 15, 1, false);
        yield return (desperateScenario, 15, 1, true);
        yield return (desperateScenario, 99, 512, false);
        yield return (desperateScenario, 100, 512, true);
    }
}
