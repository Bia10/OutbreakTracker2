using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums;

public enum InGameScenario : short
{
    [EnumMember(Value = "Unknown")]
    Unknown = -1,

    //[EnumMember(Value = "Training ground")]
    //TrainingGround = 0,

    [EnumMember(Value = "End of the road")]
    EndOfTheRoad = 6,

    [EnumMember(Value = "Underbelly")]
    Underbelly = 10,

    [EnumMember(Value = "Desperate times")]
    DesperateTimes = 15,

    [EnumMember(Value = "Showdown 1")]
    Showdown1 = 21,

    [EnumMember(Value = "Showdown 2")]
    Showdown2 = 22,

    [EnumMember(Value = "Showdown 3")]
    Showdown3 = 23,

    [EnumMember(Value = "Flashback")]
    Flashback = 26,

    [EnumMember(Value = "Elimination 3")]
    Elimination3 = 27,

    [EnumMember(Value = "Elimination 1")]
    Elimination1 = 29,

    [EnumMember(Value = "Elimination 2")]
    Elimination2 = 30,

    [EnumMember(Value = "Wild things")]
    WildThings = 40
}