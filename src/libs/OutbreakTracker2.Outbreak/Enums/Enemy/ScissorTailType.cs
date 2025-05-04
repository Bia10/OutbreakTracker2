using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums.Enemy;

public enum ScissorTailType : byte
{
    [EnumMember(Value = "Scissor Tail")]
    ScissorTail = 0,

    [EnumMember(Value = "Scissor Tail Purple")]
    ScissorTailPurple = 1
}