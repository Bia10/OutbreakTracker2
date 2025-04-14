using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums;

public enum GameFile : byte
{
    [EnumMember(Value = "Unknown")]
    Unknown = 0,

    [EnumMember(Value = "File One")]
    FileOne = 1,

    [EnumMember(Value = "File Two")]
    FileTwo = 2
}
