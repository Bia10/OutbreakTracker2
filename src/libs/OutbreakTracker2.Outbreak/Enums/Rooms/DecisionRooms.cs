using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums.Rooms;

public enum DecisionsRooms : short
{
    [EnumMember(Value = "Spawning/Scenario Cleared")]
    Spawning = 0,

    [EnumMember(Value = "West Waterway")]
    WestWaterway = 1,

    [EnumMember(Value = "East Waterway")]
    EastWaterway = 2,

    [EnumMember(Value = "Access Way A")]
    AccessWayA = 3,

    [EnumMember(Value = "Shed")]
    Shed = 4,

    [EnumMember(Value = "Water Works Management Office")]
    WaterWorksManagementOffice = 5,

    [EnumMember(Value = "Water Tank")]
    WaterTank = 6,

    [EnumMember(Value = "Access Way B")]
    AccessWayB = 7,

    [EnumMember(Value = "Underground Tunnel")]
    UndergroundTunnel = 8,

    [EnumMember(Value = "Restroom")]
    Restroom = 9,

    [EnumMember(Value = "Water Purifying Facility")]
    WaterPurifyingFacility = 10,

    [EnumMember(Value = "Quality Assurance Testing Room")]
    QualityAssuranceTestingRoom = 11,

    [EnumMember(Value = "B4F Corridor")]
    B4F_Corridor = 36,

    [EnumMember(Value = "Access Waterway")]
    AccessWaterway = 37,

    [EnumMember(Value = "Old Subway Rail Siding")]
    OldSubwayRailSiding = 38,

    [EnumMember(Value = "Old Subway Tunnel")]
    OldSubwayTunnel = 50,

    [EnumMember(Value = "South Car")]
    SouthCar = 51,

    [EnumMember(Value = "North Car")]
    NorthCar = 52,

    [EnumMember(Value = "T-Shaped Passway")]
    TShapedPassway = 54,

    [EnumMember(Value = "Control Room")]
    ControlRoom = 55,

    [EnumMember(Value = "Underground Management Office")]
    UndergroundManagementOffice = 56,

    [EnumMember(Value = "Spare Power Supply Room")]
    SparePowerSupplyRoom = 57,

    [EnumMember(Value = "Store Room")]
    StoreRoom = 58,

    [EnumMember(Value = "Air Exhaust Tower Inside Wall")]
    AirExhaustTowerInsideWall = 59,

    [EnumMember(Value = "Air Exhaust Tower Access Way")]
    AirExhaustTowerAccessWay = 60,

    [EnumMember(Value = "Air Exhaust Tower Lower Part")]
    AirExhaustTowerLowerPart = 61,

    [EnumMember(Value = "Air Exhaust Tower Elevator")]
    AirExhaustTowerElevator = 62,

    [EnumMember(Value = "Air Exhaust Tower B1 Level")]
    AirExhaustTowerB1Level = 63,

    [EnumMember(Value = "Air Exhaust Tower Station")]
    AirExhaustTowerStation = 65,

    [EnumMember(Value = "Back Square")]
    BackSquare = 70,

    [EnumMember(Value = "Pier")]
    Pier = 71,

    [EnumMember(Value = "Unloading Passage")]
    UnloadingPassage = 72,

    [EnumMember(Value = "Canal")]
    Canal = 73,

    [EnumMember(Value = "Front Square")]
    FrontSquare = 74,

    [EnumMember(Value = "B2F Passage Elevator")]
    B2F_PassageElevator = 76,

    [EnumMember(Value = "Study Room")]
    StudyRoom = 77,

    [EnumMember(Value = "1F Elevator Passway")]
    F1_ElevatorPassway = 78,

    [EnumMember(Value = "Entrance Hall")]
    EntranceHall = 80,

    [EnumMember(Value = "Students Affairs Office")]
    StudentsAffairsOffice = 81,

    [EnumMember(Value = "General Manager's Room")]
    GeneralManagersRoom = 82,

    [EnumMember(Value = "Waiting Room")]
    WaitingRoom = 83,

    [EnumMember(Value = "1F Passage A")]
    F1_PassageA = 84,

    [EnumMember(Value = "1F Passage B")]
    F1_PassageB = 85,

    [EnumMember(Value = "Testing Passage A")]
    TestingPassageA = 86,

    [EnumMember(Value = "President Room")]
    PresidentRoom = 87,

    [EnumMember(Value = "Drawings Room")]
    DrawingsRoom = 88,

    [EnumMember(Value = "2F Passage")]
    F2_Passage = 89,

    [EnumMember(Value = "Art Safe Room")]
    ArtSafeRoom = 91,

    [EnumMember(Value = "Testing Passage B")]
    TestingPassageB = 93,

    [EnumMember(Value = "3F Passage Elevator")]
    F3_PassageElevator = 94,

    [EnumMember(Value = "Experiments Preparation Room")]
    ExperimentsPreparationRoom = 95,

    [EnumMember(Value = "Experiment Room")]
    ExperimentRoom = 96,

    [EnumMember(Value = "Machine Storage Room")]
    MachineStorageRoom = 97,

    [EnumMember(Value = "Second Hall")]
    SecondHall = 98
}