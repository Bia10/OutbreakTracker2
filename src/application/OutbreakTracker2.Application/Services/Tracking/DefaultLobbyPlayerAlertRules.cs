using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Tracking;

internal static class DefaultLobbyPlayerAlertRules
{
    public static void Register(IEntityTracker<DecodedLobbyRoomPlayer> lobbyPlayers)
    {
        lobbyPlayers.AddRule(
            new PredicateAlertRule<DecodedLobbyRoomPlayer>(
                (cur, prev) => cur.IsEnabled && !(prev?.IsEnabled ?? false),
                cur => new AlertNotification(
                    "Player Joined Lobby",
                    $"{cur.CharacterName} joined the lobby.",
                    AlertLevel.Info
                )
            )
        );

        lobbyPlayers.AddRule(
            new PredicateAlertRule<DecodedLobbyRoomPlayer>(
                (cur, prev) => !cur.IsEnabled && (prev?.IsEnabled ?? false),
                cur => new AlertNotification(
                    "Player Left Lobby",
                    $"{cur.CharacterName} left the lobby.",
                    AlertLevel.Info
                )
            )
        );
    }
}
