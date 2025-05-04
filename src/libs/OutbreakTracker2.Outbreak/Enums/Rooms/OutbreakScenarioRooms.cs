using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums.Rooms;

public enum OutbreakRooms : short
{
    [EnumMember(Value = "Spawning/Scenario Cleared")]
    Spawning = 0,

    [EnumMember(Value = "J's Bar")]
    JsBar = 1,

    [EnumMember(Value = "Stairs Between 1F and 2F")]
    Stairs1F2F = 2,

    [EnumMember(Value = "Woman's Bathroom")]
    WomensBathroom = 3,

    [EnumMember(Value = "Men's Bathroom")]
    MensBathroom = 4,

    [EnumMember(Value = "Staff Room")]
    StaffRoom = 5,

    [EnumMember(Value = "Locker Room")]
    LockerRoom = 6,

    [EnumMember(Value = "Drawings Room")]
    DrawingsRoom = 7,

    [EnumMember(Value = "Owner's Room")]
    OwnersRoom = 8,

    [EnumMember(Value = "Break Room")]
    BreakRoom = 10,

    [EnumMember(Value = "Liquor Room")]
    LiquorRoom = 11,

    [EnumMember(Value = "Wine Room")]
    WineRoom = 12,

    [EnumMember(Value = "Stairs Between 3F and Rooftop")]
    Stairs3FRooftop = 13,

    [EnumMember(Value = "Rooftop")]
    Rooftop = 14,

    [EnumMember(Value = "Storage Room")]
    StorageRoom = 15,

    [EnumMember(Value = "Top Floor of The Apartment")]
    TopFloorApartment = 16,

    [EnumMember(Value = "1F of The Apartment")]
    F1Apartment = 17,

    [EnumMember(Value = "In Front of J's Bar")]
    FrontJsBar = 18,

    [EnumMember(Value = "Behind The Apartment")]
    BehindApartment = 19,

    [EnumMember(Value = "Slope Along The Canal")]
    SlopeCanal = 20,

    [EnumMember(Value = "Tunnel")]
    Tunnel = 21,

    [EnumMember(Value = "In Front of Apple Inn")]
    FrontAppleInn = 22,

    [EnumMember(Value = "Behind The Residential Area")]
    BehindResidential = 23,

    [EnumMember(Value = "Footbridge")]
    Footbridge = 24,

    [EnumMember(Value = "Main Street")]
    MainStreet = 25,

    [EnumMember(Value = "Unknown outbreak room")]
    Unknown = 255
}
