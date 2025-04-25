namespace OutbreakTracker2.Outbreak.Common;

public static class PercentageFormatter
{
    public static T GetPercentage<T>(T numerator, T denominator) where T : struct
    {
        if (IsZero(denominator))
        {
            Console.WriteLine($"Division by zero detected by denominator of type: {typeof(T).Name})");
            return default;
        }

        double num = Convert.ToDouble(numerator);
        double den = Convert.ToDouble(denominator);
        double ratio = num / den;

        return (T)Convert.ChangeType(ratio, typeof(T));
    }

    public static string GetPercentage<T>(T numerator, T denominator, int decimalPlaces) where T : struct
    {
        if (IsZero(denominator))
        {
            Console.WriteLine($"Division by zero detected by denominator of type: {typeof(T).Name})");
            return "0.00%";
        }

        double num = Convert.ToDouble(numerator);
        double den = Convert.ToDouble(denominator);
        double ratio = num / den;

        return ratio.ToString($"P{decimalPlaces}");
    }

    private static bool IsZero<T>(T value) where T : struct
        => EqualityComparer<T>.Default.Equals(value, default);
}
