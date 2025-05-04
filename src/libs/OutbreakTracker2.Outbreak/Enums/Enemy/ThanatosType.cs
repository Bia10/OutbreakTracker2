using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums.Enemy;

public enum ThanatosType : byte
{
    [EnumMember(Value = "Thanatos")]
    Thanatos = 0,

    [EnumMember(Value = "Thanatos R")]
    ThanatosR = 1
}