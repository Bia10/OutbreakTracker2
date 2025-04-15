using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums;

internal enum CharacterBaseType : byte
{
    [EnumMember(Value = "Kevin")]
    Kevin = 0,

    [EnumMember(Value = "Mark")]
    Mark = 1,

    [EnumMember(Value = "Jim")]
    Jim = 2,

    [EnumMember(Value = "George")]
    Rebecca = 3,

    [EnumMember(Value = "David")]
    Chris = 4,

    [EnumMember(Value = "Alyssa")]
    Jill = 5,

    [EnumMember(Value = "Yoko")]
    Carlos = 6,

    [EnumMember(Value = "Cindy")]
    Cindy = 7,

    [EnumMember(Value = "Unknown")]
    Unknown = 255
}
