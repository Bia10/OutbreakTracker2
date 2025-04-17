using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums;

internal enum CharacterNpcHealth : byte
{
    [EnumMember(Value = "500 - 800")]
    HpRange1 = 0,

    [EnumMember(Value = "900 - 1200")]
    HpRange2 = 1,

    [EnumMember(Value = "1300 - 1600")]
    HpRange3 = 2,

    [EnumMember(Value = "2000 - 2500")]
    HpRange4 = 3,

    [EnumMember(Value = "2600 - 3000")]
    HpRange5 = 4,

    [EnumMember(Value = "3100 - 3500")]
    HpRange6 = 5,

    [EnumMember(Value = "3600 - 4000")]
    HpRange7 = 6,

    [EnumMember(Value = "Unknown HpRange")]
    Unknown = 255
}
