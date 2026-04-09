using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Tracking;

internal static class DefaultLobbySlotAlertRules
{
    public static void Register(IEntityTracker<DecodedLobbySlot> lobbySlots)
    {
        lobbySlots.AddRule(
            new PredicateAlertRule<DecodedLobbySlot>(
                (cur, prev) => cur.IsPassProtected && !(prev?.IsPassProtected ?? false),
                cur => new AlertNotification(
                    "Lobby Password Set",
                    $"Slot {cur.SlotNumber} \"{cur.Title}\" is now password-protected.",
                    AlertLevel.Info
                )
            )
        );

        lobbySlots.AddRule(
            new PredicateAlertRule<DecodedLobbySlot>(
                (cur, prev) =>
                    cur.MaxPlayers > 0 && cur.CurPlayers >= cur.MaxPlayers && (prev?.CurPlayers ?? 0) < cur.MaxPlayers,
                cur => new AlertNotification(
                    "Lobby Full",
                    $"Slot {cur.SlotNumber} \"{cur.Title}\" is full ({cur.CurPlayers}/{cur.MaxPlayers}).",
                    AlertLevel.Info
                )
            )
        );

        lobbySlots.AddRule(
            new PredicateAlertRule<DecodedLobbySlot>(
                (cur, prev) => prev is not null && !string.Equals(cur.Status, prev.Status, StringComparison.Ordinal),
                cur => new AlertNotification(
                    "Lobby Status Changed",
                    $"Slot {cur.SlotNumber} \"{cur.Title}\" status is now {cur.Status}.",
                    AlertLevel.Info
                )
            )
        );
    }
}
