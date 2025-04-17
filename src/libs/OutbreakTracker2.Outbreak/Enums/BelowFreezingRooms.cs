using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums;

public enum BelowFreezingRooms : short
{
    [EnumMember(Value = "Spawning/Scenario Cleared")]
    Spawning = 0,

    [EnumMember(Value = "Underground Tunnel")]
    UndergroundTunnel = 1,

    [EnumMember(Value = "Platform")]
    Platform = 2,

    [EnumMember(Value = "B7F Experiments Room")]
    B7F_ExperimentsRoom = 3,

    [EnumMember(Value = "B7F South Passway")]
    B7F_SouthPassway = 4,

    [EnumMember(Value = "B7F Chemical Disposal Room")]
    B7F_ChemicalDisposalRoom = 5,

    [EnumMember(Value = "B7F East Passway")]
    B7F_EastPassway = 6,

    [EnumMember(Value = "B7F Chemical Storage")]
    B7F_ChemicalStorage = 8,

    [EnumMember(Value = "B6F Security Office")]
    B6F_SecurityOffice = 9,

    [EnumMember(Value = "B6F South Passway")]
    B6F_SouthPassway = 10,

    [EnumMember(Value = "B6F East Passway")]
    B6F_EastPassway = 13,

    [EnumMember(Value = "B6F Break Room")]
    B6F_BreakRoom = 15,

    [EnumMember(Value = "Lift")]
    Lift = 17,

    [EnumMember(Value = "Turntable Car")]
    TurntableCar = 18,

    [EnumMember(Value = "B5F Passway of Area B")]
    B5F_PasswayAreaB = 19,

    [EnumMember(Value = "B5F Computer Room")]
    B5F_ComputerRoom = 20,

    [EnumMember(Value = "B5F Emergency Passage")]
    B5F_EmergencyPassage = 21,

    [EnumMember(Value = "B5F Passway of Area C")]
    B5F_PasswayAreaC = 22,

    [EnumMember(Value = "B4F Turn Table")]
    B4F_TurnTable = 23,

    [EnumMember(Value = "Main Shaft")]
    MainShaft = 24,

    [EnumMember(Value = "B4F Passway of West Area")]
    B4F_PasswayWestArea = 25,

    [EnumMember(Value = "Duct")]
    Duct = 26,

    [EnumMember(Value = "B4F Passway of East Area")]
    B4F_PasswayEastArea = 27,

    [EnumMember(Value = "B4F Low Temperature Experimental Room")]
    B4F_LowTempExperimentalRoom = 28,

    [EnumMember(Value = "B4F Culture Room")]
    B4F_CultureRoom = 30,

    [EnumMember(Value = "Marshaling yard")]
    MarshalingYard = 32,
}
