using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums.LobbyRoom;

public enum RoomStatus : byte
{
    [EnumMember(Value = "Unknown")]
    Unknown = 255,

    [EnumMember(Value = "Waiting")]
    Easy = 0,

    [EnumMember(Value = "In Game")]
    Normal = 1,

    [EnumMember(Value = "Full")]
    Hard = 2,

    // TODO: states below are just guesses

    [EnumMember(Value = "Creating room")]
    VeryHard = 3,

    [EnumMember(Value = "Hosting room")]
    Nightmare = 4
}