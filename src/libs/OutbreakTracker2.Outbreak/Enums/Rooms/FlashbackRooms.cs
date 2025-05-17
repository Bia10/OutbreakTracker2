using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums.Rooms;

public enum FlashbackRooms : short
{
    [EnumMember(Value = "Spawning/Scenario Cleared")]
    Spawning = 0,

    [EnumMember(Value = "In Front of cabin")]
    InFrontOfCabin = 1,

    [EnumMember(Value = "Cabin")]
    Cabin = 2,

    [EnumMember(Value = "Mountain path")]
    MountainPath = 3,

    [EnumMember(Value = "Suspension bridge")]
    SuspensionBridge = 4,

    [EnumMember(Value = "Hospital back gate")]
    HospitalBackGate = 5,

    [EnumMember(Value = "Main building 1F hall")]
    MainBuilding1FHall = 6,

    [EnumMember(Value = "Reception office")]
    ReceptionOffice = 7,

    [EnumMember(Value = "Examination room")]
    ExaminationRoom = 8,

    [EnumMember(Value = "Auxiliary building 1F hall")]
    AuxiliaryBuilding1FHall = 9,

    [EnumMember(Value = "Locker room")]
    LockerRoom = 10,

    [EnumMember(Value = "Auxiliary building North hall")]
    AuxiliaryBuildingNorthHall = 11,

    [EnumMember(Value = "Storage room")]
    StorageRoom = 12,

    [EnumMember(Value = "Pharmacy")]
    Pharmacy = 14,

    [EnumMember(Value = "Auxiliary building B1F hall")]
    AuxiliaryBuildingB1FHall = 15,

    [EnumMember(Value = "Intensive care unit")]
    IntensiveCareUnit = 16,

    [EnumMember(Value = "Main building 2F hall")]
    MainBuilding2FHall = 17,

    [EnumMember(Value = "Room 201")]
    Room201 = 18,

    [EnumMember(Value = "Room 202")]
    Room202 = 19,

    [EnumMember(Value = "Room 203")]
    Room203 = 20,

    [EnumMember(Value = "Administrator's office")]
    AdministratorsOffice = 21,

    [EnumMember(Value = "Auxiliary building 2F hall")]
    AuxiliaryBuilding2FHall = 22,

    [EnumMember(Value = "Auxiliary building 3F")]
    AuxiliaryBuilding3F = 23,

    [EnumMember(Value = "Maintenance access route")]
    MaintenanceAccessRoute = 24,

    [EnumMember(Value = "Main building rooftop")]
    MainBuildingRooftop = 25,

    [EnumMember(Value = "Auxiliary building 5F")]
    AuxiliaryBuilding5F = 26,

    [EnumMember(Value = "Auxiliary building rooftop")]
    AuxiliaryBuildingRooftop = 27,

    [EnumMember(Value = "Big suspension bridge")]
    BigSuspensionBridge = 29,

    [EnumMember(Value = "River bank")]
    RiverBank = 30
}
