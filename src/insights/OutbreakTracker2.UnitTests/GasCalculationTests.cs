namespace OutbreakTracker2.UnitTests;

[TestFixture]
public class GasCalculationTests
{
    private int GasRandom { get; set; }

    private int CalculateGasRandomOrderDisplay_Original()
    {
        return GasRandom switch
        {
            > 0 and < 10 => 1,
            >= 10 and < 20 => 2,
            >= 20 and < 30 => 3,
            >= 30 and < 40 => 4,
            >= 40 and < 50 => 5,
            >= 50 and < 60 => 6,
            >= 60 and < 70 => 7,
            >= 70 and < 80 => 8,
            >= 80 and < 90 => 9,
            >= 90 and < 100 => 10,
            >= 100 and < 110 => 11,
            >= 110 and < 120 => 12,
            >= 120 and < 130 => 13,
            >= 130 and < 140 => 14,
            >= 140 and < 150 => 15,
            >= 150 and < 160 => 16,
            >= 160 and < 170 => 17,
            >= 170 and < 180 => 18,
            >= 180 and < 190 => 19,
            >= 190 and < 200 => 20,
            >= 200 and < 210 => 21,
            >= 210 and < 220 => 22,
            >= 220 and < 230 => 23,
            >= 230 and < 240 => 24,
            >= 240 and < 255 => 25,
            _ => -1,
        };
    }

    private int CalculateGasRandomOrderDisplay_Algorithmic()
    {
        return GasRandom switch
        {
            > 0 and < 240 => (GasRandom / 10) + 1,
            >= 240 and < 255 => 25,
            _ => -1,
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
        Assert.That(
            algorithmicResult,
            Is.EqualTo(originalResult),
            $"Mismatch for GasRandom = {gasRandomValue}"
        );
    }

    private static IEnumerable<int> GetGasRandomValuesToTest()
    {
        yield return -10; // Below min
        yield return 0; // Min boundary for default
        yield return 255; // Max boundary for default
        yield return 300; // Above max

        for (int i = 0; i < 260; i++)
            yield return i;

        yield return 1; // Start of first range
        yield return 9; // End of first range
        yield return 10; // Start of second range
        yield return 19;
        yield return 20;
        yield return 239; // End of 230-239 range
        yield return 240; // Start of 240-254 range
        yield return 254; // End of 240-254 range
    }
}
