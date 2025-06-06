using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Services.PlayerTracking;

public interface IPlayerStateTracker
{
    Observable<PlayerStateChangeEventArgs> PlayerStateChanges { get; }

    void PublishPlayerUpdate(DecodedInGamePlayer player);
}