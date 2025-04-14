using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums;

public enum SlotPass : byte
{
    [EnumMember(Value = "False")]
    NoPass = 0,

    [EnumMember(Value = "True")]
    Pass = 1
}