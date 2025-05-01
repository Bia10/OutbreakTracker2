using FastEnumUtility;

namespace OutbreakTracker2.Outbreak.Utility;

public static class EnumUtility
{
    public static string GetEnumString<TEnum>(object value, TEnum defaultValue)
        where TEnum : struct, Enum
    {
        try
        {
            if (!FastEnum.TryParse(value.ToString(), out TEnum result))
                return defaultValue.GetEnumMemberValue() ?? defaultValue.ToString();

            string? enumValue = result.GetEnumMemberValue();
            if (!string.IsNullOrEmpty(enumValue))
                return enumValue;

            return defaultValue.GetEnumMemberValue() ?? defaultValue.ToString();
        }
        catch (Exception ex)
        {
            return defaultValue.GetEnumMemberValue() ?? defaultValue.ToString();
        }
    }

    public static bool TryParseByValueOrMember<TEnum>(string value, out TEnum result)
        where TEnum : struct, Enum
    {
        if (FastEnum.TryParse(value, out result))
            return true;

        foreach (TEnum enumValue in FastEnum.GetValues<TEnum>())
        {
            string? memberValue = enumValue.GetEnumMemberValue();
            if (!string.Equals(value, memberValue, StringComparison.Ordinal))
                continue;

            result = enumValue;
            return true;
        }

        result = default;
        return false;
    }
}