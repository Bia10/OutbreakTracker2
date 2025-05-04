using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums.Rooms;

public enum HellfireRooms : short
{
    [EnumMember(Value = "Spawning/Scenario Cleared")]
    Spawning = 0,

    [EnumMember(Value = "Apple Inn Square")]
    AppleInnSquare = 27,

    [EnumMember(Value = "Boiler Room")]
    BoilerRoom = 28,

    [EnumMember(Value = "Corridor")]
    Corridor = 29,

    [EnumMember(Value = "Room 101")]
    Room101 = 30,

    [EnumMember(Value = "North West Passage 1")]
    NorthWestPassage1 = 31,

    [EnumMember(Value = "Owner's Room")]
    OwnersRoom = 32,

    [EnumMember(Value = "Power Supply Room")]
    PowerSupplyRoom = 33,

    [EnumMember(Value = "Lounge Stairs")]
    LoungeStairs = 34,

    [EnumMember(Value = "Apple Inn Front Lobby")]
    AppleInnFrontLobby = 35,

    [EnumMember(Value = "Room 102")]
    Room102 = 36,

    [EnumMember(Value = "Room 103")]
    Room103 = 37,

    [EnumMember(Value = "Room 104")]
    Room104 = 38,

    [EnumMember(Value = "Room 201")]
    Room201 = 40,

    [EnumMember(Value = "North West Passage 2")]
    NorthWestPassage2 = 41,

    [EnumMember(Value = "Store Room")]
    StoreRoom = 42,

    [EnumMember(Value = "Security Office")]
    SecurityOffice = 43,

    [EnumMember(Value = "Room 202")]
    Room202 = 44,

    [EnumMember(Value = "Room 204")]
    Room204 = 45,

    [EnumMember(Value = "Boiler Management Office")]
    BoilerManagementOffice = 46,

    [EnumMember(Value = "North West Passage 3")]
    NorthWestPassage3 = 47,

    [EnumMember(Value = "Room 301")]
    Room301 = 48,

    [EnumMember(Value = "Linen Room")]
    LinenRoom = 49,

    [EnumMember(Value = "Room 30*")]
    Room30Star = 50,

    [EnumMember(Value = "Room 306")]
    Room306 = 51
}
