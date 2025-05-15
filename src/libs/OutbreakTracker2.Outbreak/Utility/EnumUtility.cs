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

    public static string GetRoomName(this InGameScenario scenarioName, short roomId)
        => scenarioName switch
        {
            InGameScenario.Unknown => $"Unknown roomName for id: {roomId}",
            InGameScenario.TrainingGround => GetEnumString(roomId, TrainingGroundRooms.Spawning),
            InGameScenario.EndOfTheRoad => GetEnumString(roomId, EndOfTheRoadRooms.Spawning),
            InGameScenario.Underbelly => GetEnumString(roomId, UnderbellyRooms.Spawning),
            InGameScenario.DesperateTimes => GetEnumString(roomId, DesperateTimesRooms.Spawning),
            InGameScenario.Showdown1 => GetEnumString(roomId, ShowdownRooms.Spawning),
            InGameScenario.Showdown2 => GetEnumString(roomId, ShowdownRooms.Spawning),
            InGameScenario.Showdown3 => GetEnumString(roomId, ShowdownRooms.Spawning),
            InGameScenario.Flashback => GetEnumString(roomId, FlashbackRooms.Spawning),
            InGameScenario.Elimination3 => GetEnumString(roomId, Elimination3Rooms.Spawning),
            InGameScenario.Elimination1 => GetEnumString(roomId, Elimination1Rooms.Spawning),
            InGameScenario.Elimination2 => GetEnumString(roomId, Elimination2Rooms.Spawning),
            InGameScenario.WildThings => GetEnumString(roomId, WildThingsRooms.Spawning),
            InGameScenario.Outbreak => GetEnumString(roomId, OutbreakRooms.Spawning),
            InGameScenario.Hellfire => GetEnumString(roomId, HellfireRooms.Spawning),
            InGameScenario.TheHive => GetEnumString(roomId, HiveRooms.Spawning),
            InGameScenario.BelowFreezingPoint => GetEnumString(roomId, BelowFreezingRooms.Spawning),
            InGameScenario.DecisionsDecisions => GetEnumString(roomId, DecisionsRooms.Spawning),
            _ => throw new ArgumentOutOfRangeException(nameof(scenarioName), scenarioName, null)
        };
}