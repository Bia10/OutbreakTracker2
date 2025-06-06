using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.PlayerTracking;

public abstract class PlayerStateTrackingRule
{
    public abstract bool ShouldTrigger(DecodedInGamePlayer currentPlayer, DecodedInGamePlayer lastKnownPlayerState);
    public abstract PlayerStateChangeEventArgs CreateNotification(DecodedInGamePlayer currentPlayer);
}