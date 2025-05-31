using OutbreakTracker2.Outbreak.Common;

namespace OutbreakTracker2.Outbreak.Offsets;

internal static class LobbyRoomPlayerOffsets
{
    public static readonly (nint[] File1, nint[] File2) PlayerEnabled = ([FileOnePtrs.LobbyRoomPlayerEnabledOffset], [FileTwoPtrs.LobbyRoomPlayerEnabledOffset]);
    public static readonly (nint[] File1, nint[] File2) PlayerNpcType = ([FileOnePtrs.LobbyRoomPlayerNpcTypeOffset], [FileTwoPtrs.LobbyRoomPlayerNpcTypeOffset]);
    public static readonly (nint[] File1, nint[] File2) PlayerNameId = ([FileOnePtrs.LobbyRoomPlayerNameIdOffset], [FileTwoPtrs.LobbyRoomPlayerNameIdOffset]);
}