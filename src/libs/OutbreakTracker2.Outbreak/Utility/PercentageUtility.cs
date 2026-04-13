using System.Diagnostics;

namespace OutbreakTracker2.Outbreak.Utility;

public static class PercentageUtility
{
    public static double GetPercentage(double numerator, double denominator, int decimalPlaces = 2)
    {
        Debug.Assert(denominator != 0.0, "Percentage denominator must be non-zero.");

        if (denominator == 0.0)
            throw new ArgumentOutOfRangeException(
                nameof(denominator),
                denominator,
                "Percentage denominator must be non-zero."
            );

        double percentage = numerator / denominator * 100.0;
        return Math.Round(percentage, decimalPlaces);
    }
}
