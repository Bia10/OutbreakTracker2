using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums;

public enum GameVersion : short
{
    [EnumMember(Value = "Unknown")]
    Unknown = 0,

    [EnumMember(Value = "DVD-ROM")]
    DvdRom = 17,

    [EnumMember(Value = "HDD")]
    Hdd = 18
}