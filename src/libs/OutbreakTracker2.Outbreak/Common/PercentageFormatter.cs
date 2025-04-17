namespace OutbreakTracker2.Outbreak.Common;

public static class PercentageFormatter
{
    public static string GetPercentage<T>(T numerator, T denominator, int decimalPlaces = 2) where T : struct
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

    public static T GetPercentage<T>(T numerator, T denominator) where T : struct
    {
        if (IsZero(denominator))
            throw new DivideByZeroException($"Division by zero detected by denominator of type: {typeof(T).Name})");

        double num = Convert.ToDouble(numerator);
        double den = Convert.ToDouble(denominator);
        double ratio = num / den;

        return (T)Convert.ChangeType(ratio, typeof(T));
    }

    private static bool IsZero<T>(T value) where T : struct
        => EqualityComparer<T>.Default.Equals(value, default);
}
