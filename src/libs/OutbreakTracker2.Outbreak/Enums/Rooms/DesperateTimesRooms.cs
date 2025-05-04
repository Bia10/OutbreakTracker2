using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums.Rooms;

public enum DesperateTimesRooms : short
{
    [EnumMember(Value = "Spawning/Scenario Cleared")]
    Spawning = 0,

    [EnumMember(Value = "Main Hall")]
    MainHall = 1,

    [EnumMember(Value = "Reception desk")]
    ReceptionDesk = 2,

    [EnumMember(Value = "Front Gate")]
    FrontGate = 3,

    [EnumMember(Value = "1F lobby")]
    F1_Lobby = 4,

    [EnumMember(Value = "East office")]
    EastOffice = 5,

    [EnumMember(Value = "Stairwell")]
    Stairwell = 6,

    [EnumMember(Value = "2F East hall")]
    F2_EastHall = 7,

    [EnumMember(Value = "Rooftop")]
    Rooftop = 8,

    [EnumMember(Value = "1F East hall")]
    F1_EastHall = 9,

    [EnumMember(Value = "Night-duty room")]
    NightDutyRoom = 10,

    [EnumMember(Value = "B1F East hall")]
    B1F_EastHall = 11,

    [EnumMember(Value = "Autopsy room")]
    AutopsyRoom = 12,

    [EnumMember(Value = "Parking garage")]
    ParkingGarage = 13,

    [EnumMember(Value = "B1F West hall")]
    B1F_WestHall = 14,

    [EnumMember(Value = "Kennel")]
    Kennel = 15,

    [EnumMember(Value = "Substation room")]
    SubstationRoom = 16,

    [EnumMember(Value = "Holding cells")]
    HoldingCells = 18,

    [EnumMember(Value = "Hallway")]
    Hallway = 20,

    [EnumMember(Value = "Interrogation room")]
    InterrogationRoom = 21,

    [EnumMember(Value = "Parking garage ramp")]
    ParkingGarageRamp = 23,

    [EnumMember(Value = "Waiting room")]
    WaitingRoom = 27
}
