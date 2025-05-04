using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums.Enemy;

public enum TyrantType : byte
{
    [EnumMember(Value = "Tyrant")]
    Tyrant = 0,

    [EnumMember(Value = "Tyrant R")]
    TyrantR = 1,

    [EnumMember(Value = "Tyrant C")]
    TyrantC = 3
}