using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums.Rooms;

public enum Elimination3Rooms : short
{
    [EnumMember(Value = "Spawning/Scenario Cleared")]
    Spawning = 0,

    [EnumMember(Value = "J's Bar")]
    JsBar = 1,

    [EnumMember(Value = "Abandoned Hospital's Reception Office")]
    AbandonedHospitalsReceptionOffice = 2,

    [EnumMember(Value = "Path InFront of Observation Deck")]
    PathInFrontOfObservationDeck = 3,

    [EnumMember(Value = "Apple Inn Square")]
    AppleInnSquare = 4,

    [EnumMember(Value = "North Car")]
    NorthCar = 5,

    [EnumMember(Value = "B2F Stairwell")]
    B2FStairwell = 6,

    [EnumMember(Value = "Show Animals' Boarding House")]
    ShowAnimalsBoardingHouse = 7,

    [EnumMember(Value = "RPD's East Office")]
    RpDsEastOffice = 8,

    [EnumMember(Value = "B6F East Passway")]
    B6FEastPassway = 9,

    [EnumMember(Value = "Stairwell to Observation Mezzanine")]
    StairwellToObservationMezzanine = 10,

    [EnumMember(Value = "Underground Tunnel")]
    UndergroundTunnel = 11,

    [EnumMember(Value = "Examination Room")]
    ExaminationRoom = 12,

    [EnumMember(Value = "Subway's B2F Passage")]
    SubwaysB2FPassage = 13,

    [EnumMember(Value = "Abandoned Hospital's Locker Room")]
    AbandonedHospitalsLockerRoom = 14,

    [EnumMember(Value = "Main Shaft")]
    MainShaft = 15,

    [EnumMember(Value = "1F of The Apartment")]
    F1OfTheApartment = 16,

    [EnumMember(Value = "Parking garage")]
    ParkingGarage = 17,

    [EnumMember(Value = "Drainage Area")]
    DrainageArea = 18,

    [EnumMember(Value = "Raccoon Hospital's Hall")]
    RaccoonHospitalsHall = 19,

    [EnumMember(Value = "Room 306")]
    Room306 = 20,

    [EnumMember(Value = "Pier")]
    Pier = 21
}
