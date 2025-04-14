using OutbreakTracker2.Outbreak.Common;

namespace OutbreakTracker2.Outbreak.Offsets;

internal class LobbySlotOffsets
{
    public static readonly (nint[] File1, nint[] File2) Index = ([], []);
    public static readonly (nint[] File1, nint[] File2) CurPlayers = ([FileOnePtrs.LobbySlotPlayer], [FileTwoPtrs.LobbySlotPlayer]);
    public static readonly (nint[] File1, nint[] File2) MaxPlayers =  ([FileOnePtrs.LobbySlotMaxPlayer], [FileTwoPtrs.LobbySlotMaxPlayer]);
    public static readonly (nint[] File1, nint[] File2) Status =  ([FileOnePtrs.LobbySlotStatus], [FileTwoPtrs.LobbySlotStatus]);
    public static readonly (nint[] File1, nint[] File2) Pass = ([FileOnePtrs.LobbySlotPass], [FileTwoPtrs.LobbySlotPass]);
    public static readonly (nint[] File1, nint[] File2) ScenarioId = ([FileOnePtrs.LobbySlotScenarioID], [FileTwoPtrs.LobbySlotScenarioID]);
    public static readonly (nint[] File1, nint[] File2) Version = ([FileOnePtrs.LobbySlotVersion], [FileTwoPtrs.LobbySlotVersion]);
    public static readonly (nint[] File1, nint[] File2) Title = ([FileOnePtrs.LobbySlotTitle], [FileTwoPtrs.LobbySlotTitle]);
}
