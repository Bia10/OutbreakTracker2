using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums.LobbyRoom;

public enum RoomStatus : byte
{
    [EnumMember(Value = "Unknown")]
    Unknown = 255,

    [EnumMember(Value = "Waiting")]
    Waiting = 0,

    [EnumMember(Value = "In Game")]
    InGame = 1, // also active on the lobby selection screen after 12

    [EnumMember(Value = "Full")]
    Full = 2, // also active on the lobby selection screen after 1

    [EnumMember(Value = "Creating room")]
    CreatingRoom = 3, // also be active on the lobby selection screen

    [EnumMember(Value = "Hosting room")]
    HostingRoom = 4,

    [EnumMember(Value = "Launching room")]
    LaunchingRoom = 5

    // 12 unknown
    // Scenario Ended, before scenario meeting room appears

    // 212 unknown
    // Scenario Loaded, before first cinematic intro, before scenario status changes to 2
}