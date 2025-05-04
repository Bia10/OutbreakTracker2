using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums.Enemy;

public enum ZombieType : byte
{
    [EnumMember(Value = "Unknown Zombie 0")]
    Unknown0 = 0,

    [EnumMember(Value = "G.Zombie")]
    GZombie = 31,

    [EnumMember(Value = "Kevin")]
    Kevin = 70,

    [EnumMember(Value = "Mark")]
    Mark = 71,

    [EnumMember(Value = "Jim")]
    Jim = 72,

    [EnumMember(Value = "George")]
    George = 73,

    [EnumMember(Value = "David")]
    David = 74,

    [EnumMember(Value = "Alyssa")]
    Alyssa = 75,

    [EnumMember(Value = "Yoko")]
    Yoko = 76,

    [EnumMember(Value = "Cindy")]
    Cindy = 77,

    [EnumMember(Value = "Bob")]
    Bob = 80,

    [EnumMember(Value = "Will")]
    Will = 85
}