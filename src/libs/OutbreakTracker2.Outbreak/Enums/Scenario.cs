using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums;

public enum Scenario : short
{
    [EnumMember(Value = "Unknown(0)")]
    Unknown = 0,

    [EnumMember(Value = "Outbreak")]
    Outbreak = 1,

    [EnumMember(Value = "Hellfire")]
    Hellfire = 2,

    [EnumMember(Value = "End of the road")]
    EndOfTheRoad = 6,

    [EnumMember(Value = "Underbelly")]
    Underbelly = 10,

    [EnumMember(Value = "Desperate times")]
    DesperateTimes = 15,

    [EnumMember(Value = "Training ground")]
    TrainingGround = 20,

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

    [EnumMember(Value = "The hive")]
    TheHive = 28,

    [EnumMember(Value = "Elimination 1")]
    Elimination1 = 29,

    [EnumMember(Value = "Elimination 2")]
    Elimination2 = 30,

    [EnumMember(Value = "Below freezing point")]
    BelowFreezingPoint = 35,

    [EnumMember(Value = "Wild things")]
    WildThings = 40,

    [EnumMember(Value = "Decisions, decisions")]
    DecisionsDecisions = 41
}