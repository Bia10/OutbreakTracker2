using OutbreakTracker2.Outbreak.Models;
using R3;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OutbreakTracker2.Application.Services.PlayerTracking;

public sealed class PlayerStateTracker : IPlayerStateTracker
{
    private readonly Subject<DecodedInGamePlayer> _playerUpdateSubject = new();
    private readonly Dictionary<Ulid, DecodedInGamePlayer> _lastKnownPlayerStates = new();

    public Observable<PlayerStateChangeEventArgs> PlayerStateChanges { get; }

    public PlayerStateTracker()
    {
        IReadOnlyList<PlayerStateTrackingRule> trackingRules = BuildDefaultRules();

        PlayerStateChanges = _playerUpdateSubject
            .SelectMany(currentPlayer =>
            {
                if (currentPlayer is null)
                    return Observable.Empty<PlayerStateChangeEventArgs>();

                List<PlayerStateChangeEventArgs> notifications = [];

                _lastKnownPlayerStates.TryGetValue(currentPlayer.Id, out DecodedInGamePlayer? lastKnownStateForThisPlayer);

                DecodedInGamePlayer previousState = lastKnownStateForThisPlayer ?? new DecodedInGamePlayer { Id = currentPlayer.Id };

                IEnumerable<PlayerStateChangeEventArgs> notificationsToAdd =
                    (from rule in trackingRules
                        where rule.ShouldTrigger(currentPlayer, previousState)
                        select rule.CreateNotification(currentPlayer)
                    ).ToList();

                notifications.AddRange(notificationsToAdd);

                _lastKnownPlayerStates[currentPlayer.Id] = currentPlayer;

                return notifications.ToObservable();
            });
    }

    public void PublishPlayerUpdate(DecodedInGamePlayer player)
        => _playerUpdateSubject.OnNext(player);

    private static IReadOnlyList<PlayerStateTrackingRule> BuildDefaultRules()
    {
        IReadOnlyList<PlayerStateTrackingRule> playerTrackingRules = new PlayerStateTrackerBuilder()
            .TrackCondition("danger", (_, charName) => ($"{charName} is now in DANGER!", ToastType.Error))
            //.TrackCondition("down", (_, charName) => ($"{charName} is DOWN!", ToastType.Error))
            .TrackCondition("gas", (_, charName) => ($"{charName} is gassed!", ToastType.Warning))
            .TrackStatus("Dead", (_, charName) => ($"{charName} has DIED!", ToastType.Error))
            .TrackStatus("Zombie", (_, charName) => ($"{charName} turned into a ZOMBIE!", ToastType.Error))
            .TrackStatus("Down", (_, charName) => ($"{charName} is now DOWNED!", ToastType.Warning))
            .TrackStatus("Bleed", (_, charName) => ($"{charName} is now BLEEDING!", ToastType.Warning))
            .TrackGeneralChange(
                (current, last) => current.CurHealth <= 0 && last.CurHealth > 0,
                current => new PlayerStateChangeEventArgs($"{current.Name} health dropped to 0!", "Player Died", ToastType.Error))
            .BuildRules();

        return playerTrackingRules;
    }
}