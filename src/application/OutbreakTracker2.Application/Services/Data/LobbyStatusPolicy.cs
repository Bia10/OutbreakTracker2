using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Data;

internal static class LobbyStatusPolicy
{
    private static readonly IReadOnlySet<string> ActiveStatuses = new HashSet<string>(StringComparer.Ordinal)
    {
        "Waiting",
        "In Game",
        "Full",
        "Creating room",
        "Hosting room",
        "Launching room",
    };

    public static bool IsActive(in DecodedLobbyRoom lobbyRoom) =>
        lobbyRoom.CurPlayer is >= 0 and <= GameConstants.MaxPlayers
        && lobbyRoom.MaxPlayer is >= 2 and <= GameConstants.MaxPlayers
        && ActiveStatuses.Contains(lobbyRoom.Status);
}
