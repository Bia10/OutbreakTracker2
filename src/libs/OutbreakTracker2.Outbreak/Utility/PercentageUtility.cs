using System.Diagnostics;
using System.Globalization;

namespace OutbreakTracker2.Outbreak.Utility;

public static class PercentageUtility
{
    public static T GetPercentage<T>(T numerator, T denominator, int? decimalPlaces = 2)
        where T : struct
    {
        if (IsZero(numerator) && IsZero(denominator))
            return default;

        if (IsZero(denominator))
        {
            Trace.TraceWarning(
                $"Division by zero: denominator of type {typeof(T).Name} is zero (numerator: {numerator}). Returning default."
            );
            return default;
        }

        double num = Convert.ToDouble(numerator, CultureInfo.InvariantCulture);
        double den = Convert.ToDouble(denominator, CultureInfo.InvariantCulture);
        double ratio = num / den;
        double percentage = ratio * 100;

        if (decimalPlaces.HasValue)
            percentage = Math.Round(percentage, decimalPlaces.Value);

        T convertedResult = (T)Convert.ChangeType(percentage, typeof(T), CultureInfo.InvariantCulture);

        return convertedResult;
    }

    private static bool IsZero<T>(T value)
        where T : struct => EqualityComparer<T>.Default.Equals(value, default);
}
