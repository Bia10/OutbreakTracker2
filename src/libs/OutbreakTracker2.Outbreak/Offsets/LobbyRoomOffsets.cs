using OutbreakTracker2.Outbreak.Common;

namespace OutbreakTracker2.Outbreak.Offsets;

internal class LobbyRoomOffsets
{
    public static readonly (nint[] File1, nint[] File2) MaxPlayers = ([FileOnePtrs.LobbyRoomMaxPlayer], [FileTwoPtrs.LobbyRoomMaxPlayer]);
    public static readonly (nint[] File1, nint[] File2) Difficulty =  ([FileOnePtrs.LobbyRoomDifficulty], [FileTwoPtrs.LobbyRoomDifficulty]);
    public static readonly (nint[] File1, nint[] File2) Status =  ([FileOnePtrs.LobbyRoomStatus], [FileTwoPtrs.LobbyRoomStatus]);
    public static readonly (nint[] File1, nint[] File2) ScenarioId =  ([FileOnePtrs.LobbyRoomScenarioId], [FileTwoPtrs.LobbyRoomScenarioId]);
    public static readonly (nint[] File1, nint[] File2) Time =  ([FileOnePtrs.LobbyRoomTime], [FileTwoPtrs.LobbyRoomTime]);
    public static readonly (nint[] File1, nint[] File2) CurPlayers = ([FileOnePtrs.LobbyRoomCurPlayer], [FileTwoPtrs.LobbyRoomCurPlayer]);
}
