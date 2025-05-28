using FastEnumUtility;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Enums.Rooms;
using System.Runtime.CompilerServices;

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
        if (FastEnum.TryParse(value.AsSpan(), out result))
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

    // This serves as a routing hub for potentially boxed value types.
    private static bool TryParseByValueOrMember<TEnum>(object? value, out TEnum result)
        where TEnum : struct, Enum
    {
        result = default;

        switch (value)
        {
            case null: return false;
            // If the object is already the correct enum type (no boxing needed)
            case TEnum enumInstance: result = enumInstance; return true;
            // If the object is a string, use the string overload directly
            case string str: return TryParseByValueOrMember(str, out result);
 // Unbox byte and call the byte overload
            case byte b: return TryParseByValueOrMember(b, out result);
 // Unbox short and call the short overload
            case short s: return TryParseByValueOrMember(s, out result);
 // Unbox int and call the int overload
            case int i: return TryParseByValueOrMember(i, out result);
        }

        // For unhandled value types (like char, float, custom structs) or reference types,
        // ToString() will still cause boxing if 'value' is a value type.
        string? valueString = value.ToString();
        if (string.IsNullOrEmpty(valueString))
            return false;

        if (FastEnum.TryParse(valueString.AsSpan(), out result))
        {
            if (FastEnum.IsDefined(result))
                return true;
        }

        foreach (TEnum enumValue in FastEnum.GetValues<TEnum>())
        {
            string? memberValue = enumValue.GetEnumMemberValue();
            if (string.IsNullOrEmpty(memberValue) || !string.Equals(valueString, memberValue, StringComparison.OrdinalIgnoreCase))
                continue;

            result = enumValue;
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParseByValueOrMember<TEnum>(byte value, out TEnum result)
        where TEnum : struct, Enum
    {
        result = default;

        Span<char> charBuffer = stackalloc char[3];
        if (value.TryFormat(charBuffer, out int charsWritten))
        {
            if (FastEnum.TryParse(charBuffer[..charsWritten], out result) && FastEnum.IsDefined(result))
                return true;
        }

        return TryParseByValueOrMember(value.ToString(), out result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParseByValueOrMember<TEnum>(short value, out TEnum result)
        where TEnum : struct, Enum
    {
        result = default;

        Span<char> charBuffer = stackalloc char[6];
        if (value.TryFormat(charBuffer, out int charsWritten))
        {
            if (FastEnum.TryParse(charBuffer[..charsWritten], out result) && FastEnum.IsDefined(result))
                return true;
        }

        return TryParseByValueOrMember(value.ToString(), out result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParseByValueOrMember<TEnum>(int value, out TEnum result)
        where TEnum : struct, Enum
    {
        result = default;

        Span<char> charBuffer = stackalloc char[11];
        if (value.TryFormat(charBuffer, out int charsWritten))
        {
            if (FastEnum.TryParse(charBuffer[..charsWritten], out result) && FastEnum.IsDefined(result))
                return true;
        }

        return TryParseByValueOrMember(value.ToString(), out result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParseByValueOrMember<TEnum>(TEnum value, out TEnum result)
        where TEnum : struct, Enum
    {
        result = value;
        return FastEnum.IsDefined(value);
    }

    public static string GetRoomName(this Scenario scenarioName, int roomId)
        => scenarioName switch
        {
            Scenario.Unknown => $"Unknown roomName for id: {roomId}",
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
            _ => throw new ArgumentOutOfRangeException(nameof(scenarioName), scenarioName, null)
        };
}