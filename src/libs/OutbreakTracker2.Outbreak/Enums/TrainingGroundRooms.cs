using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums;

public enum TrainingGroundRooms : short
{
    [EnumMember(Value = "Spawning/Scenario Cleared")]
    Spawning = 0,

    [EnumMember(Value = "J's BAR")]
    JsBar = 1,

    [EnumMember(Value = "Stairs between 1F and 2F")]
    Stairs1F2F = 2,

    [EnumMember(Value = "Staff room")]
    StaffRoom = 5,

    [EnumMember(Value = "Liquor room")]
    LiquorRoom = 11,

    [EnumMember(Value = "Stairs between 3F and the rooftop")]
    Stairs3FRooftop = 13,

    [EnumMember(Value = "Rooftop")]
    Rooftop = 14
}
