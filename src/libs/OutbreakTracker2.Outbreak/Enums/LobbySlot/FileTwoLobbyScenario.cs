using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums.LobbySlot;

public enum FileTwoLobbyScenario : short
{
    [EnumMember(Value = "Unknown")]
    Unknown = -1,

    [EnumMember(Value = "Training ground")]
    TrainingGround = 0,

    [EnumMember(Value = "Wild things")]
    WildThings = 1,

    [EnumMember(Value = "Underbelly")]
    Underbelly = 2,

    [EnumMember(Value = "Flashback")]
    Flashback = 3,

    [EnumMember(Value = "Desperate times")]
    DesperateTimes = 4,

    [EnumMember(Value = "End of the road")]
    EndOfTheRoad = 5,

    [EnumMember(Value = "Elimination 1")]
    Elimination1 = 6,

    [EnumMember(Value = "Elimination 2")]
    Elimination2 = 7,

    [EnumMember(Value = "Elimination 3")]
    Elimination3 = 8,

    [EnumMember(Value = "Showdown 1")]
    Showdown1 = 9,

    [EnumMember(Value = "Showdown 2")]
    Showdown2 = 10,

    [EnumMember(Value = "Showdown 3")]
    Showdown3 = 11
}