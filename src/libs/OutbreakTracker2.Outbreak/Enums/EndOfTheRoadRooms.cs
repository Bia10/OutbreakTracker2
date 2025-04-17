using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums;

public enum EndOfTheRoadRooms : short
{
    [EnumMember(Value = "Spawning/Scenario Cleared")]
    Spawning = 0,

    [EnumMember(Value = "Waiting room")]
    WaitingRoom = 1,

    [EnumMember(Value = "Central Passage 1")]
    CentralPassage1 = 2,

    [EnumMember(Value = "Central Passage 2")]
    CentralPassage2 = 3,

    [EnumMember(Value = "West Passage")]
    WestPassage = 4,

    [EnumMember(Value = "Central Passage 3")]
    CentralPassage3 = 5,

    [EnumMember(Value = "Laser Emission Room")]
    LaserEmissionRoom = 6,

    [EnumMember(Value = "Examination Room")]
    ExaminationRoom = 7,

    [EnumMember(Value = "Experimentation Chamber")]
    ExperimentationChamber = 8,

    [EnumMember(Value = "Observation Mezzanine")]
    ObservationMezzanine = 9,

    [EnumMember(Value = "Stairwell")]
    Stairwell = 10,

    [EnumMember(Value = "Reference Room")]
    ReferenceRoom = 11,

    [EnumMember(Value = "East Passage 1")]
    EastPassage1 = 12,

    [EnumMember(Value = "Mainframe")]
    Mainframe = 13,

    [EnumMember(Value = "Central Passage 4")]
    CentralPassage4 = 14,

    [EnumMember(Value = "Special Research Room")]
    SpecialResearchRoom = 15,

    [EnumMember(Value = "East Passage 2")]
    EastPassage2 = 16,

    [EnumMember(Value = "East Passage 3")]
    EastPassage3 = 17,

    [EnumMember(Value = "Nursery")]
    Nursery = 18,

    [EnumMember(Value = "East Exit")]
    EastExit = 19,

    [EnumMember(Value = "Passage In Front of Elevator")]
    PassageInFrontOfElevator = 20,

    [EnumMember(Value = "Maintenance Passage 1")]
    MaintenancePassage1 = 28,

    [EnumMember(Value = "Floodgate Control Room")]
    FloodgateControlRoom = 29,

    [EnumMember(Value = "Underground Waterworks")]
    UndergroundWaterworks = 30,

    [EnumMember(Value = "Maintenance Room")]
    MaintenanceRoom = 31,

    [EnumMember(Value = "Drainage Area")]
    DrainageArea = 32,

    [EnumMember(Value = "Maintenance Passage 2")]
    MaintenancePassage2 = 37,

    [EnumMember(Value = "Break Room")]
    BreakRoom = 38,

    [EnumMember(Value = "North Waterway")]
    NorthWaterway = 39,

    [EnumMember(Value = "Emergency Materials Storage")]
    EmergencyMaterialsStorage = 45,

    [EnumMember(Value = "Maintenance Passage 3")]
    MaintenancePassage3 = 46,

    [EnumMember(Value = "Old Waterway")]
    OldWaterway = 48,

    [EnumMember(Value = "Tunnel")]
    Tunnel = 52,

    [EnumMember(Value = "In Front of Apple Inn")]
    InFrontOfAppleInn = 53,

    [EnumMember(Value = "Behind The Residential Area")]
    BehindTheResidentialArea = 54,

    [EnumMember(Value = "Footbridge")]
    Footbridge = 55,

    [EnumMember(Value = "Main Street North")]
    MainStreetNorth = 56,

    [EnumMember(Value = "Main Street South")]
    MainStreetSouth = 57,

    [EnumMember(Value = "Construction Site")]
    ConstructionSite = 58,

    [EnumMember(Value = "Under The Highway Overpass")]
    UnderTheHighwayOverpass = 59,

    [EnumMember(Value = "Office Building Warehouse")]
    OfficeBuildingWarehouse = 60,

    [EnumMember(Value = "Office Building 1F")]
    OfficeBuilding1F = 61,

    [EnumMember(Value = "Office Building Stairwell")]
    OfficeBuildingStairwell = 62,

    [EnumMember(Value = "Rooftop - Elevated Highway")]
    Rooftop_ElevatedHighway = 63,

    [EnumMember(Value = "Inside The Helicopter")]
    InsideTheHelicopter = 65,

    [EnumMember(Value = "Apple Inn Front Lobby")]
    AppleInnFrontLobby = 66
}
