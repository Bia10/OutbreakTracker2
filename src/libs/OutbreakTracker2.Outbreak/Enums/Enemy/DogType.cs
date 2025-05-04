using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums.Enemy;

public enum DogType : byte
{
    [EnumMember(Value = "Unknown Dog 0")]
    Unknown0 = 0,

    [EnumMember(Value = "Cerberus")]
    Cerberus = 1,

    [EnumMember(Value = "Shepherd")]
    Shepherd = 4
}