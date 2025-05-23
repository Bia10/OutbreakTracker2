using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums.Character;

public enum CharacterBaseType : byte
{
    [EnumMember(Value = "Kevin")]
    Kevin = 0,

    [EnumMember(Value = "Mark")]
    Mark = 1,

    [EnumMember(Value = "Jim")]
    Jim = 2,

    [EnumMember(Value = "George")]
    George = 3,

    [EnumMember(Value = "David")]
    David = 4,

    [EnumMember(Value = "Alyssa")]
    Alyssa = 5,

    [EnumMember(Value = "Yoko")]
    Yoko = 6,

    [EnumMember(Value = "Cindy")]
    Cindy = 7,

    [EnumMember(Value = "Unknown")]
    Unknown = 255
}
