using System.Diagnostics;

namespace OutbreakTracker2.Outbreak.Utility;

public static class PercentageUtility
{
    public static double GetPercentage(double numerator, double denominator, int decimalPlaces = 2)
    {
        if (numerator == 0.0 && denominator == 0.0)
            return 0.0;

        if (denominator == 0.0)
        {
            Trace.TraceWarning($"Division by zero: denominator is zero (numerator: {numerator}). Returning 0.");
            return 0.0;
        }

        double percentage = numerator / denominator * 100.0;
        return Math.Round(percentage, decimalPlaces);
    }
}
