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
        {
            return defaultValue.GetEnumMemberValue() ?? defaultValue.ToString();
        }

        string valueString = value.ToString()!;

        if (string.IsNullOrEmpty(valueString))
        {
            return defaultValue.GetEnumMemberValue() ?? defaultValue.ToString();
        }

        if (FastEnum.TryParse(valueString, out TEnum result))
        {
            if (FastEnum.IsDefined(result))
            {
                string? enumValue = result.GetEnumMemberValue();

                if (!string.IsNullOrEmpty(enumValue))
                {
                    return enumValue;
                }
                else
                {
                    return defaultValue.GetEnumMemberValue() ?? defaultValue.ToString();
                }
            }
            else
            {
                return defaultValue.GetEnumMemberValue() ?? defaultValue.ToString();
            }
        }
        else
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

     public static string GetRoomName(this InGameScenario scenarioName, short roomID) => scenarioName switch
    {
        InGameScenario.Unknown => GetEnumString(roomID, TrainingGroundRooms.Spawning),
        InGameScenario.TrainingGround => GetEnumString(roomID, TrainingGroundRooms.Spawning),
        InGameScenario.EndOfTheRoad => GetEnumString(roomID, EndOfTheRoadRooms.Spawning),
        InGameScenario.Underbelly => GetEnumString(roomID, UnderbellyRooms.Spawning),
        InGameScenario.DesperateTimes => GetEnumString(roomID, DesperateTimesRooms.Spawning),
        InGameScenario.Showdown1 => GetEnumString(roomID, ShowdownRooms.Spawning),
        InGameScenario.Showdown2 => GetEnumString(roomID, ShowdownRooms.Spawning),
        InGameScenario.Showdown3 => GetEnumString(roomID, ShowdownRooms.Spawning),
        InGameScenario.Flashback => GetEnumString(roomID, FlashbackRooms.Spawning),
        InGameScenario.Elimination3 => GetEnumString(roomID, Elimination3Rooms.Spawning),
        InGameScenario.Elimination1 => GetEnumString(roomID, Elimination1Rooms.Spawning),
        InGameScenario.Elimination2 => GetEnumString(roomID, Elimination2Rooms.Spawning),
        InGameScenario.WildThings => GetEnumString(roomID, WildThingsRooms.Spawning),
        _ => throw new ArgumentOutOfRangeException(nameof(scenarioName), scenarioName, null)
    };
}