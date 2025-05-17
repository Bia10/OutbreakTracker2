using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums.Rooms;

public enum Elimination1Rooms : short
{
    [EnumMember(Value = "Spawning/Scenario Cleared")]
    Spawning = 0,

    [EnumMember(Value = "University Entrance Hall")]
    UniversityEntranceHall = 1,

    [EnumMember(Value = "Room 30*")]
    Room30Star = 2,

    [EnumMember(Value = "Office Building Warehouse")]
    OfficeBuildingWarehouse = 3,

    [EnumMember(Value = "Zoo's Connecting Passage")]
    ZoosConnectingPassage = 4,

    [EnumMember(Value = "Doctor Station")]
    DoctorStation = 5,

    [EnumMember(Value = "B7F South Passway")]
    B7FSouthPassway = 6,

    [EnumMember(Value = "(Subway) Break Room")]
    SubwayBreakRoom = 7,

    [EnumMember(Value = "Lion Stage")]
    LionStage = 8,

    [EnumMember(Value = "RPD Rooftop")]
    RpdRooftop = 9,

    [EnumMember(Value = "1F RPD East Hall")]
    F1RpdEastHall = 10,

    [EnumMember(Value = "J's Bar Staff Room")]
    JsBarStaffRoom = 11,

    [EnumMember(Value = "1F Abandoned Hospital")]
    F1AbandonedHospital = 12,

    [EnumMember(Value = "B6F Security Office")]
    B6FSecurityOffice = 13,

    [EnumMember(Value = "Pump Room")]
    PumpRoom = 14,

    [EnumMember(Value = "Experimentation Chamber")]
    ExperimentationChamber = 15,

    [EnumMember(Value = "T-Shaped Passway")]
    ShapedPassway = 16,

    [EnumMember(Value = "Security Office")]
    SecurityOffice = 17,

    [EnumMember(Value = "Cabin")]
    Cabin = 18
}
