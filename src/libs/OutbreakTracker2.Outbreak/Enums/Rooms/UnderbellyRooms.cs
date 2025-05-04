using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums.Rooms;

public enum UnderbellyRooms : short
{
    [EnumMember(Value = "Spawning/Scenario Cleared")]
    Spawning = 0,

    [EnumMember(Value = "West entrance")]
    WestEntrance = 1,

    [EnumMember(Value = "West ticket gate")]
    WestTicketGate = 2,

    [EnumMember(Value = "West concourse")]
    WestConcourse = 3,

    [EnumMember(Value = "Storage room #2")]
    StorageRoom2 = 4,

    [EnumMember(Value = "Men's restroom (west)")]
    MensRestroomWest = 5,

    [EnumMember(Value = "Woman's restroom (west)")]
    WomansRestroomWest = 6,

    [EnumMember(Value = "Employee passage")]
    EmployeePassage = 7,

    [EnumMember(Value = "Storage room")]
    StorageRoom = 8,

    [EnumMember(Value = "Control room")]
    ControlRoom = 9,

    [EnumMember(Value = "Breaker room")]
    BreakerRoom = 10,

    [EnumMember(Value = "Break room")]
    BreakRoom = 11,

    [EnumMember(Value = "Woman's staff restroom")]
    WomansStaffRestroom = 12,

    [EnumMember(Value = "Men's staff restroom")]
    MensStaffRestroom = 13,

    [EnumMember(Value = "Stairwell")]
    Stairwell = 15,

    [EnumMember(Value = "East entrance")]
    EastEntrance = 16,

    [EnumMember(Value = "East ticket gate")]
    EastTicketGate = 17,

    [EnumMember(Value = "East concourse")]
    EastConcourse = 18,

    [EnumMember(Value = "Men's restroom (east)")]
    MensRestroomEast = 20,

    [EnumMember(Value = "Woman's restroom (east)")]
    WomansRestroomEast = 21,

    [EnumMember(Value = "B2F passage")]
    B2F_Passage = 22,

    [EnumMember(Value = "Refuse dump")]
    RefuseDump = 23,

    [EnumMember(Value = "Pump room")]
    PumpRoom = 24,

    [EnumMember(Value = "Emergency power room")]
    EmergencyPowerRoom = 25,

    [EnumMember(Value = "Underground emergency passage")]
    UndergroundEmergencyPassage = 29,

    [EnumMember(Value = "Subway car")]
    SubwayCar = 30,

    [EnumMember(Value = "Platform")]
    Platform = 31,

    [EnumMember(Value = "East tunnel")]
    EastTunnel = 32,

    [EnumMember(Value = "Ventilation tower")]
    VentilationTower = 33
}
