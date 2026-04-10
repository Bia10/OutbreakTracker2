using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Services.Tracking;

public interface ITrackerRegistry
{
    IEntityTracker<DecodedEnemy> Enemies { get; }
    IEntityTracker<DecodedDoor> Doors { get; }
    IEntityTracker<DecodedInGamePlayer> Players { get; }
    IEntityTracker<DecodedLobbySlot> LobbySlots { get; }

    Observable<AlertNotification> AllAlerts { get; }
}
