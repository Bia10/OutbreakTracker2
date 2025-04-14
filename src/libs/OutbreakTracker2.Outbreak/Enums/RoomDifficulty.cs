using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums;

public enum RoomDifficulty : byte
{
    [EnumMember(Value = "Unknown")]
    Unknown = 255,

    [EnumMember(Value = "Easy")]
    Easy = 0,

    [EnumMember(Value = "Normal")]
    Normal = 1,

    [EnumMember(Value = "Hard")]
    Hard = 2,

    [EnumMember(Value = "Very hard")]
    VeryHard = 3
}