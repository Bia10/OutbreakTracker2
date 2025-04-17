using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums;

public enum HiveRooms : short
{
    [EnumMember(Value = "Spawning/Scenario Cleared")]
    Spawning = 0,

    [EnumMember(Value = "1F Passway")]
    F1_Passway = 2,

    [EnumMember(Value = "Night Reception")]
    NightReception = 3,

    [EnumMember(Value = "Hall")]
    Hall = 4,

    [EnumMember(Value = "Office")]
    Office = 5,

    [EnumMember(Value = "Doctor Station")]
    DoctorStation = 6,

    [EnumMember(Value = "Central Waiting Room")]
    CentralWaitingRoom = 7,

    [EnumMember(Value = "Room 301")]
    Room301 = 8,

    [EnumMember(Value = "3F Passway")]
    F3_Passway = 9,

    [EnumMember(Value = "Room 302")]
    Room302 = 10,

    [EnumMember(Value = "Nurse Center")]
    NurseCenter = 11,

    [EnumMember(Value = "Examination Room")]
    ExaminationRoom = 12,

    [EnumMember(Value = "Treatment Room")]
    TreatmentRoom = 13,

    [EnumMember(Value = "Locker Room")]
    LockerRoom = 14,

    [EnumMember(Value = "2F Nurse Station")]
    F2_NurseStation = 16,

    [EnumMember(Value = "2F Passway")]
    F2_Passway = 22,

    [EnumMember(Value = "Room 202")]
    Room202 = 24,

    [EnumMember(Value = "B1F Passway")]
    B1F_Passway = 25,

    [EnumMember(Value = "EV. Control Room")]
    EV_ControlRoom = 26,

    [EnumMember(Value = "B1F Reposing Room")]
    B1F_ReposingRoom = 27,

    [EnumMember(Value = "B1F South Passway")]
    B1F_SouthPassway = 28,

    [EnumMember(Value = "Waste Liquid Disposal Room")]
    WasteLiquidDisposalRoom = 29,

    [EnumMember(Value = "B2F Passway")]
    B2F_Passway = 30,

    [EnumMember(Value = "Experiment Room")]
    ExperimentRoom = 31,

    [EnumMember(Value = "Fixed Temperature Experiment Room")]
    FixedTemperatureExperimentRoom = 32,

    [EnumMember(Value = "Underpass Entrance")]
    UnderpassEntrance = 33,

    [EnumMember(Value = "Rooftop")]
    Rooftop = 34
}
