using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums;

public enum SlotStatus : byte
{
    [EnumMember(Value = "Unknown")]
    Unknown = 0,

    [EnumMember(Value = "Vacant")]
    Vacant = 1,

    [EnumMember(Value = "Busy")]
    Busy = 2,

    [EnumMember(Value = "Join in")]
    JoinIn = 3,

    [EnumMember(Value = "Full")]
    Full = 4
}