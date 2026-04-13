using System.Globalization;
using System.Runtime.CompilerServices;
using FastEnumUtility;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Enums.Rooms;

namespace OutbreakTracker2.Outbreak.Utility;

public static class EnumUtility
{
    // Note: Currently used types have overloads which won't box, tho unhandled types will box.
    public static string GetEnumString<TEnum>(object? value, TEnum defaultValue)
        where TEnum : struct, Enum
    {
        if (value is null)
            return defaultValue.GetEnumMemberValue() ?? defaultValue.ToString();

        if (TryParseByValueOrMember(value, out TEnum result) && FastEnum.IsDefined(result))
        {
            string? enumValue = result.GetEnumMemberValue();
            if (!string.IsNullOrEmpty(enumValue))
                return enumValue;
        }

        return defaultValue.GetEnumMemberValue() ?? defaultValue.ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetEnumString<TEnum>(byte value, TEnum defaultValue)
        where TEnum : struct, Enum
    {
        if (TryParseByValueOrMember(value, out TEnum result) && FastEnum.IsDefined(result))
        {
            string? enumValue = result.GetEnumMemberValue();
            if (!string.IsNullOrEmpty(enumValue))
                return enumValue;
        }

        return defaultValue.GetEnumMemberValue() ?? defaultValue.ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetEnumString<TEnum>(short value, TEnum defaultValue)
        where TEnum : struct, Enum
    {
        if (TryParseByValueOrMember(value, out TEnum result) && FastEnum.IsDefined(result))
        {
            string? enumValue = result.GetEnumMemberValue();
            if (!string.IsNullOrEmpty(enumValue))
                return enumValue;
        }

        return defaultValue.GetEnumMemberValue() ?? defaultValue.ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetEnumString<TEnum>(int value, TEnum defaultValue)
        where TEnum : struct, Enum
    {
        if (TryParseByValueOrMember(value, out TEnum result) && FastEnum.IsDefined(result))
        {
            string? enumValue = result.GetEnumMemberValue();
            if (!string.IsNullOrEmpty(enumValue))
                return enumValue;
        }

        return defaultValue.GetEnumMemberValue() ?? defaultValue.ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetEnumString<TEnum>(TEnum value, TEnum defaultValue)
        where TEnum : struct, Enum
    {
        if (FastEnum.IsDefined(value))
        {
            string? enumValue = value.GetEnumMemberValue();
            if (!string.IsNullOrEmpty(enumValue))
                return enumValue;
        }

        return defaultValue.GetEnumMemberValue() ?? defaultValue.ToString();
    }

    public static bool TryParseByValueOrMember<TEnum>(string value, out TEnum result)
        where TEnum : struct, Enum
    {
        return TryParseByValueOrMember(value.AsSpan(), out result);
    }

    private static bool TryParseByValueOrMember<TEnum>(ReadOnlySpan<char> value, out TEnum result)
        where TEnum : struct, Enum
    {
        if (FastEnum.TryParse(value, out result))
            return true;

        foreach (TEnum enumValue in FastEnum.GetValues<TEnum>())
        {
            string? memberValue = enumValue.GetEnumMemberValue();
            if (
                string.IsNullOrEmpty(memberValue)
                || !value.Equals(memberValue.AsSpan(), StringComparison.OrdinalIgnoreCase)
            )
                continue;

            result = enumValue;
            return true;
        }

        result = default;
        return false;
    }

    // This serves as a routing hub for potentially boxed value types.
    private static bool TryParseByValueOrMember<TEnum>(object? value, out TEnum result)
        where TEnum : struct, Enum
    {
        result = default;

        switch (value)
        {
            case null:
                return false;
            // If the object is already the correct enum type (no boxing needed)
            case TEnum enumInstance:
                result = enumInstance;
                return true;
            // If the object is a string, use the string overload directly
            case string str:
                return TryParseByValueOrMember(str, out result);
            // Unbox integral values and use the span-based formatting path.
            case byte b:
                return TryParseByValueOrMember(b, out result);
            case short s:
                return TryParseByValueOrMember(s, out result);
            case int i:
                return TryParseByValueOrMember(i, out result);
            case sbyte sb:
                return TryParseFormattableValue(sb, 4, out result);
            case ushort us:
                return TryParseFormattableValue(us, 5, out result);
            case uint ui:
                return TryParseFormattableValue(ui, 10, out result);
            case long l:
                return TryParseFormattableValue(l, 20, out result);
            case ulong ul:
                return TryParseFormattableValue(ul, 20, out result);
            case char ch:
                Span<char> charBuffer = stackalloc char[1];
                charBuffer[0] = ch;
                return TryParseByValueOrMember(charBuffer, out result);
            case ISpanFormattable spanFormattable:
                return TryParseSpanFormattable(spanFormattable, out result);
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParseByValueOrMember<TEnum>(byte value, out TEnum result)
        where TEnum : struct, Enum => TryParseFormattableValue(value, 3, out result);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParseByValueOrMember<TEnum>(short value, out TEnum result)
        where TEnum : struct, Enum => TryParseFormattableValue(value, 6, out result);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParseByValueOrMember<TEnum>(int value, out TEnum result)
        where TEnum : struct, Enum => TryParseFormattableValue(value, 11, out result);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParseByValueOrMember<TEnum>(TEnum value, out TEnum result)
        where TEnum : struct, Enum
    {
        result = value;
        return FastEnum.IsDefined(value);
    }

    private static bool TryParseFormattableValue<TValue, TEnum>(TValue value, int bufferLength, out TEnum result)
        where TValue : ISpanFormattable
        where TEnum : struct, Enum
    {
        Span<char> charBuffer = stackalloc char[bufferLength];
        if (value.TryFormat(charBuffer, out int charsWritten, default, CultureInfo.InvariantCulture))
            return TryParseByValueOrMember(charBuffer[..charsWritten], out result);

        result = default;
        return false;
    }

    private static bool TryParseSpanFormattable<TEnum>(ISpanFormattable value, out TEnum result)
        where TEnum : struct, Enum
    {
        Span<char> charBuffer = stackalloc char[64];
        if (value.TryFormat(charBuffer, out int charsWritten, default, CultureInfo.InvariantCulture))
            return TryParseByValueOrMember(charBuffer[..charsWritten], out result);

        result = default;
        return false;
    }

    public static string GetRoomName(this Scenario scenarioName, int roomId) =>
        scenarioName switch
        {
            Scenario.Unknown => $"Room {roomId}",
            Scenario.TrainingGround => GetEnumString(roomId, TrainingGroundRooms.Spawning),
            Scenario.EndOfTheRoad => GetEnumString(roomId, EndOfTheRoadRooms.Spawning),
            Scenario.Underbelly => GetEnumString(roomId, UnderbellyRooms.Spawning),
            Scenario.DesperateTimes => GetEnumString(roomId, DesperateTimesRooms.Spawning),
            Scenario.Showdown1 => GetEnumString(roomId, ShowdownRooms.Spawning),
            Scenario.Showdown2 => GetEnumString(roomId, ShowdownRooms.Spawning),
            Scenario.Showdown3 => GetEnumString(roomId, ShowdownRooms.Spawning),
            Scenario.Flashback => GetEnumString(roomId, FlashbackRooms.Spawning),
            Scenario.Elimination3 => GetEnumString(roomId, Elimination3Rooms.Spawning),
            Scenario.Elimination1 => GetEnumString(roomId, Elimination1Rooms.Spawning),
            Scenario.Elimination2 => GetEnumString(roomId, Elimination2Rooms.Spawning),
            Scenario.WildThings => GetEnumString(roomId, WildThingsRooms.Spawning),
            Scenario.Outbreak => GetEnumString(roomId, OutbreakRooms.Spawning),
            Scenario.Hellfire => GetEnumString(roomId, HellfireRooms.Spawning),
            Scenario.TheHive => GetEnumString(roomId, HiveRooms.Spawning),
            Scenario.BelowFreezingPoint => GetEnumString(roomId, BelowFreezingRooms.Spawning),
            Scenario.DecisionsDecisions => GetEnumString(roomId, DecisionsRooms.Spawning),
            _ => throw new ArgumentOutOfRangeException(nameof(scenarioName), scenarioName, message: null),
        };
}
