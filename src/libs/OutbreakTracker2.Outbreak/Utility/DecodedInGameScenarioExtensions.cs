using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Outbreak.Utility;

public static class DecodedInGameScenarioExtensions
{
    public static string GetGameTimeDisplay(this DecodedInGameScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        return TimeUtility.GetTimeFromFrames(scenario.FrameCounter);
    }

    public static int GetGasRandomOrderDisplay(this DecodedInGameScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        return scenario.GasRandom switch
        {
            0 => -1,
            > 0 and < 240 => (scenario.GasRandom / 10) + 1,
            >= 240 and < 255 => 25,
            _ => -1,
        };
    }
}
