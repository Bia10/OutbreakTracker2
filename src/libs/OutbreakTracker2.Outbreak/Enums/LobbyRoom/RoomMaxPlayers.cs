using System.Runtime.Serialization;

namespace OutbreakTracker2.Outbreak.Enums.LobbyRoom;

public enum RoomMaxPlayers : byte
{
    [EnumMember(Value = "2")]
    Two = 0,

    [EnumMember(Value = "3")]
    Three = 1,

    [EnumMember(Value = "4")]
    Four = 2,
}