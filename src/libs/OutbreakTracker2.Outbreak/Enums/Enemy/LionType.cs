using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums.Enemy;

public enum LionType : byte
{
    [EnumMember(Value = "Stalker")]
    Stalker = 0,

    [EnumMember(Value = "Feral")]
    Feral = 1
}