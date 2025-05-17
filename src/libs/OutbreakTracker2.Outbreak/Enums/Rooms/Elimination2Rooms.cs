using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums.Rooms;

public enum Elimination2Rooms : short
{
    [EnumMember(Value = "Spawning/Scenario Cleared")]
    Spawning = 0,

    [EnumMember(Value = "In Front of Elephant Restaurant")]
    InFrontOfElephantRestaurant = 1,

    [EnumMember(Value = "Suspension Bridge")]
    SuspensionBridge = 2,

    [EnumMember(Value = "Main Street South")]
    MainStreetSouth = 3,

    [EnumMember(Value = "B7F Experiments Room")]
    B7FExperimentsRoom = 4,

    [EnumMember(Value = "Raccoon Hospital 1F Passage")]
    RaccoonHospital1FPassage = 5,

    [EnumMember(Value = "J's Bar Rooftop")]
    JsBarRooftop = 6,

    [EnumMember(Value = "Hospital Back Gate")]
    HospitalBackGate = 7,

    [EnumMember(Value = "University's Access Way A")]
    UniversitysAccessWayA = 8,

    [EnumMember(Value = "(Subway) East Concourse")]
    SubwayEastConcourse = 9,

    [EnumMember(Value = "J's Bar Stairwell")]
    JsBarStairwell = 10,

    [EnumMember(Value = "Apple Inn's North West Passage 3")]
    AppleInnsNorthWestPassage3 = 11,

    [EnumMember(Value = "Lakeside Area")]
    LakesideArea = 12,

    [EnumMember(Value = "B5F Passway of Area B")]
    B5FPasswayOfAreaB = 13,

    [EnumMember(Value = "RPD's Front Gate")]
    RpDsFrontGate = 14,

    [EnumMember(Value = "Owner's Room")]
    OwnersRoom = 15
}
