using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Services.Tracking;

public interface ITrackerRegistry
{
    IReadOnlyEntityTracker<DecodedEnemy> Enemies { get; }
    IReadOnlyEntityTracker<DecodedDoor> Doors { get; }
    IReadOnlyEntityTracker<DecodedInGamePlayer> Players { get; }
    IReadOnlyEntityTracker<DecodedLobbySlot> LobbySlots { get; }

    IEntityChangeSource<DecodedEnemy> EnemyChanges { get; }
    IEntityChangeSource<DecodedDoor> DoorChanges { get; }
    IEntityChangeSource<DecodedInGamePlayer> PlayerChanges { get; }
    IEntityChangeSource<DecodedLobbySlot> LobbySlotChanges { get; }

    Observable<AlertNotification> AllAlerts { get; }
}
