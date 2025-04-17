using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums;

public enum WildThingsRooms : short
{
    [EnumMember(Value = "Spawning/Scenario Cleared")]
    Spawning = 0,

    [EnumMember(Value = "Elephant Restaurant")]
    ElephantRestaurant = 1,

    [EnumMember(Value = "Back alley")]
    BackAlley = 2,

    [EnumMember(Value = "In Front of Elephant Restaurant")]
    InFrontOfElephantRestaurant = 3,

    [EnumMember(Value = "South Concourse")]
    SouthConcourse = 4,

    [EnumMember(Value = "East Concourse")]
    EastConcourse = 5,

    [EnumMember(Value = "North Concourse")]
    NorthConcourse = 6,

    [EnumMember(Value = "Office")]
    Office = 7,

    [EnumMember(Value = "Inner office")]
    InnerOffice = 8,

    [EnumMember(Value = "Elephant Stage")]
    ElephantStage = 12,

    [EnumMember(Value = "Connecting passage")]
    ConnectingPassage = 14,

    [EnumMember(Value = "Supply room")]
    SupplyRoom = 15,

    [EnumMember(Value = "Terrarium Dome")]
    TerrariumDome = 16,

    [EnumMember(Value = "Show animals' boarding house")]
    ShowAnimalsBoardingHouse = 17,

    [EnumMember(Value = "Lakeside area")]
    LakesideArea = 18,

    [EnumMember(Value = "Path in Front of Observation Deck")]
    PathInFrontOfObservationDeck = 19,

    [EnumMember(Value = "Observation deck")]
    ObservationDeck = 21,

    [EnumMember(Value = "Service road")]
    ServiceRoad = 23,

    // Duplicate display string with different ID
    [EnumMember(Value = "Show animals' boarding house")]
    ShowAnimalsBoardingHouse2 = 24,  

    [EnumMember(Value = "Stage")]
    Stage = 25,

    [EnumMember(Value = "Front Gate Plaza")]
    FrontGatePlaza = 26,

    [EnumMember(Value = "Front gate")]
    FrontGate = 27
}
