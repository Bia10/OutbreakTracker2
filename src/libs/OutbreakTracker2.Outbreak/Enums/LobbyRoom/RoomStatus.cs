using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums.LobbyRoom;

public enum RoomStatus : byte
{
    [EnumMember(Value = "Unknown")]
    Unknown = 255,

    [EnumMember(Value = "Waiting")]
    Waiting = 0,

    [EnumMember(Value = "In Game")]
    InGame = 1,

    [EnumMember(Value = "Full")]
    Full = 2,

    // TODO: states below are just guesses

    [EnumMember(Value = "Creating room")]
    CreatingRoom = 3,

    [EnumMember(Value = "Hosting room")]
    HostingRoom = 4
}