namespace OutbreakTracker2.UnitTests;

[TestFixture]
public class GasRoomIdsCalculationTests
{
    private string ScenarioName { get; set; } = "desperate times";
    private int GasRandom { get; set; }
    private int GasFlag { get; set; }
    private bool IsHardOrVeryHardResult { get; set; } = true;

    private bool IsHardOrVeryHard()
    {
        return IsHardOrVeryHardResult;
    }

    private List<int>? CalculateGasRoomIdsDisplay_Original()
    {
        if (!ScenarioName.Equals("desperate times", StringComparison.Ordinal))
            return null;

        bool isHardOrVeryHard = IsHardOrVeryHard();
        List<int> roomIds = [];

        switch (GasRandom % 2)
        {
            case 0:
                switch (GasFlag)
                {
                    case 1:
                        {
                            if (isHardOrVeryHard) roomIds.Add(4);
                            roomIds.Add(14);
                            roomIds.Add(20);
                            break;
                        }
                    case 2:
                        {
                            if (isHardOrVeryHard) roomIds.Add(7);
                            roomIds.Add(10);
                            roomIds.Add(12);
                            break;
                        }
                    case 4:
                        {
                            if (isHardOrVeryHard) roomIds.Add(9);
                            roomIds.Add(13);
                            roomIds.Add(27);
                            break;
                        }
                    case 8:
                        {
                            if (isHardOrVeryHard) roomIds.Add(5);
                            roomIds.Add(7);
                            roomIds.Add(21);
                            break;
                        }
                    case 16:
                        {
                            if (isHardOrVeryHard) roomIds.Add(4);
                            roomIds.Add(10);
                            roomIds.Add(11);
                            break;
                        }
                    case 32:
                        {
                            if (isHardOrVeryHard) roomIds.Add(5);
                            roomIds.Add(15);
                            roomIds.Add(16);
                            break;
                        }
                    case 64:
                        {
                            if (isHardOrVeryHard) roomIds.Add(4);
                            roomIds.Add(11);
                            roomIds.Add(13);
                            break;
                        }
                    case 128:
                        {
                            if (isHardOrVeryHard) roomIds.Add(14);
                            roomIds.Add(15);
                            roomIds.Add(21);
                            break;
                        }
                    case 256:
                        {
                            if (isHardOrVeryHard) roomIds.Add(11);
                            roomIds.Add(20);
                            roomIds.Add(27);
                            break;
                        }
                    case 512:
                        {
                            if (isHardOrVeryHard) roomIds.Add(5);
                            roomIds.Add(9);
                            roomIds.Add(16);
                            break;
                        }
                }

                break; // break for GasRandom % 2 == 0
            case 1:
                switch (GasFlag)
                {
                    case 1:
                        {
                            if (isHardOrVeryHard) roomIds.Add(7);
                            roomIds.Add(10);
                            roomIds.Add(16);
                            break;
                        }
                    case 2:
                        {
                            if (isHardOrVeryHard) roomIds.Add(4);
                            roomIds.Add(14);
                            roomIds.Add(27);
                            break;
                        }
                    case 4:
                        {
                            if (isHardOrVeryHard) roomIds.Add(7);
                            roomIds.Add(16);
                            roomIds.Add(20);
                            break;
                        }
                    case 8:
                        {
                            if (isHardOrVeryHard) roomIds.Add(9);
                            roomIds.Add(13);
                            roomIds.Add(21);
                            break;
                        }
                    case 16:
                        {
                            if (isHardOrVeryHard) roomIds.Add(10);
                            roomIds.Add(12);
                            roomIds.Add(15);
                            break;
                        }
                    case 32:
                        {
                            if (isHardOrVeryHard) roomIds.Add(5);
                            roomIds.Add(16);
                            roomIds.Add(21);
                            break;
                        }
                    case 64:
                        {
                            if (isHardOrVeryHard) roomIds.Add(5);
                            roomIds.Add(11);
                            roomIds.Add(27);
                            break;
                        }
                    case 128:
                        {
                            if (isHardOrVeryHard) roomIds.Add(4);
                            roomIds.Add(7);
                            roomIds.Add(20);
                            break;
                        }
                    case 256:
                        {
                            if (isHardOrVeryHard) roomIds.Add(10);
                            roomIds.Add(12);
                            roomIds.Add(13);
                            break;
                        }
                    case 512:
                        {
                            if (isHardOrVeryHard) roomIds.Add(7);
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

    private List<int>? CalculateGasRoomIdsDisplay_Refactored()
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

    [Test]
    [TestCaseSource(nameof(GetGasRoomIdsTestCases))]
    public void OriginalAndRefactoredRoomIdsMethodsAreEquivalent(
        string scenario,
        int gasRandom,
        int gasFlag,
        bool isHard)
    {
        // Arrange
        ScenarioName = scenario;
        GasRandom = gasRandom;
        GasFlag = gasFlag;
        IsHardOrVeryHardResult = isHard;

        // Act
        List<int>? originalResult = CalculateGasRoomIdsDisplay_Original();
        List<int>? refactoredResult = CalculateGasRoomIdsDisplay_Refactored();

        // Assert
        Assert.That(refactoredResult is null, Is.EqualTo(originalResult is null),
            $"Mismatch null/non-null result for Scenario={scenario}, Random={gasRandom}, Flag={gasFlag}, Hard={isHard}");

        // If not null, compare the lists
        if (originalResult is not null)
        {
            Assert.That(refactoredResult, Is.EqualTo(originalResult).AsCollection,
                $"Mismatch list content for Scenario={scenario}, Random={gasRandom}, Flag={gasFlag}, Hard={isHard}");
        }
    }

    private static IEnumerable<object[]> GetGasRoomIdsTestCases()
    {
        // Case 1: ScenarioName is NOT "desperate times"
        // Should return null regardless of other inputs
        yield return ["other scenario", 0, 0, false];
        yield return ["another one", 50, 100, true];

        // Case 2: ScenarioName IS "desperate times"
        const string desperateScenario = "desperate times";
        int[] definedGasFlags = [1, 2, 4, 8, 16, 32, 64, 128, 256, 512];
        int[] undefinedGasFlags = [0, 3, 5, 1000, -1];

        foreach (int flag in definedGasFlags)
        {
            // Modulo 0
            yield return [desperateScenario, 0, flag, false]; // Use 0 for even random
            yield return [desperateScenario, 0, flag, true];

            // Modulo 1
            yield return [desperateScenario, 1, flag, false]; // Use 1 for odd random
            yield return [desperateScenario, 1, flag, true];
        }

        foreach (int flag in undefinedGasFlags)
        {
            // Modulo 0
            yield return [desperateScenario, 0, flag, false];
            yield return [desperateScenario, 0, flag, true];

            // Modulo 1
            yield return [desperateScenario, 1, flag, false];
            yield return [desperateScenario, 1, flag, true];
        }

        yield return [desperateScenario, 10, 1, false]; // Modulo 0
        yield return [desperateScenario, 10, 1, true];
        yield return [desperateScenario, 15, 1, false]; // Modulo 1
        yield return [desperateScenario, 15, 1, true];
        yield return [desperateScenario, 99, 512, false]; // Modulo 1
        yield return [desperateScenario, 100, 512, true]; // Modulo 0
    }
}