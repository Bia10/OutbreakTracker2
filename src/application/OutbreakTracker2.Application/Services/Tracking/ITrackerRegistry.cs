using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Services.Tracking;

public interface ITrackerRegistry
{
    IReadOnlyEntityTracker<DecodedEnemy> Enemies { get; }
    IReadOnlyEntityTracker<DecodedDoor> Doors { get; }
    IReadOnlyEntityTracker<DecodedInGamePlayer> Players { get; }
    IReadOnlyEntityTracker<DecodedLobbySlot> LobbySlots { get; }

    Observable<AlertNotification> AllAlerts { get; }
}
