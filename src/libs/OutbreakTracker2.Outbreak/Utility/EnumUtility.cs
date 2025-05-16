using FastEnumUtility;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Enums.Rooms;

namespace OutbreakTracker2.Outbreak.Utility;

public static class EnumUtility
{
    public static string GetEnumString<TEnum>(object? value, TEnum defaultValue)
        where TEnum : struct, Enum
    {
        if (value is null)
            return defaultValue.GetEnumMemberValue() ?? defaultValue.ToString();

        string valueString = value.ToString()!;

        if (string.IsNullOrEmpty(valueString) || !FastEnum.TryParse(valueString, out TEnum result)
                                              || !FastEnum.IsDefined(result))
            return defaultValue.GetEnumMemberValue() ?? defaultValue.ToString();

        string? enumValue = result.GetEnumMemberValue();

        if (!string.IsNullOrEmpty(enumValue))
            return enumValue;

        return defaultValue.GetEnumMemberValue() ?? defaultValue.ToString();
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