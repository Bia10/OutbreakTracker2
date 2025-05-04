using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums.Character;

internal enum CharacterHealth : byte
{
    [EnumMember(Value = "2300")]
    Kevin = 0,

    [EnumMember(Value = "3000")]
    Mark = 1,

    [EnumMember(Value = "1800")]
    Jim = 2,

    [EnumMember(Value = "2100")]
    George = 3,

    [EnumMember(Value = "2200")]
    David = 4,

    [EnumMember(Value = "2000")]
    Alyssa = 5,

    [EnumMember(Value = "1300")]
    Yoko = 6,

    [EnumMember(Value = "1500")]
    Cindy = 7,

    [EnumMember(Value = "0")]
    Unknown = 255
}
