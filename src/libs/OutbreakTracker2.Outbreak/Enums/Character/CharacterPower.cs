using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums.Character;

internal enum CharacterPower : byte
{
    [EnumMember(Value = "100%")]
    Kevin = 0,

    [EnumMember(Value = "100%")]
    Mark = 1,

    [EnumMember(Value = "100%")]
    Jim = 2,

    [EnumMember(Value = "100%")]
    George = 3,

    [EnumMember(Value = "100%")]
    David = 4,

    [EnumMember(Value = "100%")]
    Alyssa = 5,

    [EnumMember(Value = "100%")]
    Yoko = 6,

    [EnumMember(Value = "100%")]
    Cindy = 7,

    [EnumMember(Value = "0%")]
    Unknown = 255
}
