namespace OutbreakTracker2.Outbreak.Utility;

public static class PercentageUtility
{
    public static T GetPercentage<T>(T numerator, T denominator, int? decimalPlaces = 2) where T : struct
    {
        if (IsZero(numerator) && IsZero(denominator))
            return default;

        if (IsZero(denominator))
        {
            Console.WriteLine($"Division by zero detected by denominator of type: {typeof(T).Name}), numerator: {numerator}, denominator: {denominator}. Returning default value.");
            return default;
        }

        double num = Convert.ToDouble(numerator);
        double den = Convert.ToDouble(denominator);
        double ratio = num / den;
        double percentage = ratio * 100;

        if (decimalPlaces.HasValue)
            percentage = Math.Round(percentage, decimalPlaces.Value);

        T convertedResult = (T)Convert.ChangeType(percentage, typeof(T));

        return convertedResult;
    }

    private static bool IsZero<T>(T value) where T : struct
        => EqualityComparer<T>.Default.Equals(value, default);
}
