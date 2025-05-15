namespace OutbreakTracker2.UnitTests;

[TestFixture]
public class GasCalculationTests
{
    private int GasRandom { get; set; }

    private int CalculateGasRandomOrderDisplay_Original()
    {
        switch (GasRandom)
        {
            case > 0 and < 10: return 1;
            case >= 10 and < 20: return 2;
            case >= 20 and < 30: return 3;
            case >= 30 and < 40: return 4;
            case >= 40 and < 50: return 5;
            case >= 50 and < 60: return 6;
            case >= 60 and < 70: return 7;
            case >= 70 and < 80: return 8;
            case >= 80 and < 90: return 9;
            case >= 90 and < 100: return 10;
            case >= 100 and < 110: return 11;
            case >= 110 and < 120: return 12;
            case >= 120 and < 130: return 13;
            case >= 130 and < 140: return 14;
            case >= 140 and < 150: return 15;
            case >= 150 and < 160: return 16;
            case >= 160 and < 170: return 17;
            case >= 170 and < 180: return 18;
            case >= 180 and < 190: return 19;
            case >= 190 and < 200: return 20;
            case >= 200 and < 210: return 21;
            case >= 210 and < 220: return 22;
            case >= 220 and < 230: return 23;
            case >= 230 and < 240: return 24;
            case >= 240 and < 255: return 25;

            default:
                return -1;
        }
    }

    private int CalculateGasRandomOrderDisplay_Algorithmic()
    {
        return GasRandom switch
        {
            > 0 and < 240 => (GasRandom / 10) + 1,
            >= 240 and < 255 => 25,
            _ => -1
        };
    }

    [Test]
    [TestCaseSource(nameof(GetGasRandomValuesToTest))]
    public void OriginalAndAlgorithmicImplementationsAreFunctionallyEquivalent(int gasRandomValue)
    {
        // Arrange
        GasRandom = gasRandomValue;

        // Act
        int originalResult = CalculateGasRandomOrderDisplay_Original();
        int algorithmicResult = CalculateGasRandomOrderDisplay_Algorithmic();

        // Assert that the results are the same
        Assert.That(algorithmicResult, Is.EqualTo(originalResult), $"Mismatch for GasRandom = {gasRandomValue}");
    }

    private static IEnumerable<int> GetGasRandomValuesToTest()
    {
        yield return -10; // Below min
        yield return 0;   // Min boundary for default
        yield return 255; // Max boundary for default
        yield return 300; // Above max

        for (int i = 0; i < 260; i++)
            yield return i;

        yield return 1;   // Start of first range
        yield return 9;   // End of first range
        yield return 10;  // Start of second range
        yield return 19;
        yield return 20;
        yield return 239; // End of 230-239 range
        yield return 240; // Start of 240-254 range
        yield return 254; // End of 240-254 range
    }
}