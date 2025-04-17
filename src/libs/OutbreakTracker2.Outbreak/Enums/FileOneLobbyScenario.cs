using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums;

public enum FileOneLobbyScenario : short
{
    [EnumMember(Value = "Unknown")]
    Unknown = -1,

    [EnumMember(Value = "Outbreak")]
    Outbreak = 0,

    [EnumMember(Value = "Below freezing point")]
    BelowFreezingPoint = 1,

    [EnumMember(Value = "The hive")]
    TheHive = 2,

    [EnumMember(Value = "Hellfire")]
    Hellfire = 3,

    [EnumMember(Value = "Decisions, decisions")]
    DecisionsDecisions = 4
}