using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Data;

/// <summary>
/// Pull-based snapshot access. Inject this into alert rule helpers that read state synchronously
/// during entity diff evaluation.
/// </summary>
public interface IDataSnapshot
{
    DecodedDoor[] Doors { get; }
    DecodedEnemy[] Enemies { get; }
    DecodedInGamePlayer[] InGamePlayers { get; }
    DecodedInGameScenario InGameScenario { get; }
    DecodedLobbyRoom LobbyRoom { get; }
    DecodedLobbyRoomPlayer[] LobbyRoomPlayers { get; }
    DecodedLobbySlot[] LobbySlots { get; }
    bool IsAtLobby { get; }
}
