using OutbreakTracker2.Outbreak.Models;
using R3;
using System.Collections.Generic;
using System.Linq;

namespace OutbreakTracker2.App.Services.PlayerTracking;

public sealed class PlayerStateTracker : IPlayerStateTracker
{
    private readonly Subject<DecodedInGamePlayer> _playerUpdateSubject = new();
    public Observable<PlayerStateChangeEventArgs> PlayerStateChanges { get; }

    private DecodedInGamePlayer _lastKnownPlayerState = new();

    public PlayerStateTracker()
    {
        IReadOnlyList<PlayerStateTrackingRule> trackingRules = BuildDefaultRules();

        PlayerStateChanges = _playerUpdateSubject
            .SelectMany(currentPlayer =>
            {
                List<PlayerStateChangeEventArgs> notifications = [];
                IEnumerable<PlayerStateChangeEventArgs> notificationsToAdd =
                    (from rule in trackingRules
                    where rule.ShouldTrigger(currentPlayer, _lastKnownPlayerState)
                    select rule.CreateNotification(currentPlayer)
                    ).ToList();

                notifications.AddRange(notificationsToAdd);
                _lastKnownPlayerState = currentPlayer;

                return notifications.ToObservable();
            });
    }

    private static IReadOnlyList<PlayerStateTrackingRule> BuildDefaultRules()
    {
        IReadOnlyList<PlayerStateTrackingRule> playerTrackingRules = new PlayerStateTrackerBuilder()
            .TrackCondition("danger", (_, charName) => ($"{charName} is now in DANGER!", ToastType.Error))
            .TrackCondition("down", (_, charName) => ($"{charName} is DOWN!", ToastType.Error))
            .TrackCondition("gas", (_, charName) => ($"{charName} is gassed!", ToastType.Warning))
            .TrackStatus("Dead", (_, charName) => ($"{charName} has DIED!", ToastType.Error))
            .TrackStatus("Zombie", (_, charName) => ($"{charName} turned into a ZOMBIE!", ToastType.Error))
            .TrackStatus("Down", (_, charName) => ($"{charName} is now DOWNED!", ToastType.Warning))
            .TrackStatus("Bleed", (_, charName) => ($"{charName} is now BLEEDING!", ToastType.Warning))
            .TrackGeneralChange(
                (currentPlayer, lastKnownPlayerState) => currentPlayer.CurrentHealth <= 0 && lastKnownPlayerState.CurrentHealth > 0,
                (currentPlayer) => new PlayerStateChangeEventArgs($"{currentPlayer.CharacterName} health dropped to 0!", "Player Status", ToastType.Error))
            .BuildRules();

        return playerTrackingRules;
    }

    public void PublishPlayerUpdate(DecodedInGamePlayer player)
        => _playerUpdateSubject.OnNext(player);
}