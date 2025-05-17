using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums.Rooms;

public enum BelowFreezingRooms : short
{
    [EnumMember(Value = "Spawning/Scenario Cleared")]
    Spawning = 0,

    [EnumMember(Value = "Underground Tunnel")]
    UndergroundTunnel = 1,

    [EnumMember(Value = "Platform")]
    Platform = 2,

    [EnumMember(Value = "B7F Experiments Room")]
    B7FExperimentsRoom = 3,

    [EnumMember(Value = "B7F South Passway")]
    B7FSouthPassway = 4,

    [EnumMember(Value = "B7F Chemical Disposal Room")]
    B7FChemicalDisposalRoom = 5,

    [EnumMember(Value = "B7F East Passway")]
    B7FEastPassway = 6,

    [EnumMember(Value = "B7F Chemical Storage")]
    B7FChemicalStorage = 8,

    [EnumMember(Value = "B6F Security Office")]
    B6FSecurityOffice = 9,

    [EnumMember(Value = "B6F South Passway")]
    B6FSouthPassway = 10,

    [EnumMember(Value = "B6F East Passway")]
    B6FEastPassway = 13,

    [EnumMember(Value = "B6F Break Room")]
    B6FBreakRoom = 15,

    [EnumMember(Value = "Lift")]
    Lift = 17,

    [EnumMember(Value = "Turntable Car")]
    TurntableCar = 18,

    [EnumMember(Value = "B5F Passway of Area B")]
    B5FPasswayAreaB = 19,

    [EnumMember(Value = "B5F Computer Room")]
    B5FComputerRoom = 20,

    [EnumMember(Value = "B5F Emergency Passage")]
    B5FEmergencyPassage = 21,

    [EnumMember(Value = "B5F Passway of Area C")]
    B5FPasswayAreaC = 22,

    [EnumMember(Value = "B4F Turn Table")]
    B4FTurnTable = 23,

    [EnumMember(Value = "Main Shaft")]
    MainShaft = 24,

    [EnumMember(Value = "B4F Passway of West Area")]
    B4FPasswayWestArea = 25,

    [EnumMember(Value = "Duct")]
    Duct = 26,

    [EnumMember(Value = "B4F Passway of East Area")]
    B4FPasswayEastArea = 27,

    [EnumMember(Value = "B4F Low Temperature Experimental Room")]
    B4FLowTempExperimentalRoom = 28,

    [EnumMember(Value = "B4F Culture Room")]
    B4FCultureRoom = 30,

    [EnumMember(Value = "Marshaling yard")]
    MarshalingYard = 32
}
