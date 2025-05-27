using OutbreakTracker2.Outbreak.Models;
using R3;
using System.Collections.Generic;

namespace OutbreakTracker2.App.Services.PlayerTracking;

public class PlayerStateTracker : IPlayerStateTracker
{
    private readonly Subject<DecodedInGamePlayer> _playerUpdateSubject = new();

    public Observable<PlayerStateChangeEventArgs> PlayerStateChanges { get; }

    private DecodedInGamePlayer _lastKnownPlayerState = new();

    public PlayerStateTracker()
    {
        PlayerStateChanges = _playerUpdateSubject
            .SelectMany(currentPlayer =>
            {
                List<PlayerStateChangeEventArgs> notifications = [];

                if (currentPlayer.Condition != _lastKnownPlayerState.Condition)
                {
                    var (message, type) = GetConditionToastDetails(currentPlayer.Condition, currentPlayer.CharacterName);
                    if (!string.IsNullOrWhiteSpace(message))
                        notifications.Add(new PlayerStateChangeEventArgs(message, "Condition Update", type));
                }

                if (currentPlayer.Status != _lastKnownPlayerState.Status)
                {
                    var (message, type) = GetStatusToastDetails(currentPlayer.Status, currentPlayer.CharacterName);
                    if (!string.IsNullOrWhiteSpace(message))
                        notifications.Add(new PlayerStateChangeEventArgs(message, "Status Update", type));
                }

                if (currentPlayer.InGame != _lastKnownPlayerState.InGame)
                {
                    notifications.Add(currentPlayer.InGame
                        ? new PlayerStateChangeEventArgs($"{currentPlayer.CharacterName} has joined the game.",
                            "Player Status", ToastType.Info)
                        : new PlayerStateChangeEventArgs($"{currentPlayer.CharacterName} has left the game.",
                            "Player Status", ToastType.Info));
                }

                if (currentPlayer.CurrentHealth <= 0 && _lastKnownPlayerState.CurrentHealth > 0)
                {
                    notifications.Add(currentPlayer.InGame
                        ? new PlayerStateChangeEventArgs($"{currentPlayer.CharacterName} is downed!", "Player Status",
                            ToastType.Warning)
                        : new PlayerStateChangeEventArgs($"{currentPlayer.CharacterName} has died!", "Player Status",
                            ToastType.Error));
                }

                _lastKnownPlayerState = currentPlayer;

                return notifications.ToObservable();
            });
    }

    public void PublishPlayerUpdate(DecodedInGamePlayer player)
        => _playerUpdateSubject.OnNext(player);

    private static (string message, ToastType type) GetConditionToastDetails(string rawCondition, string characterName)
    {
        return rawCondition.ToLower() switch
        {
            "fine" => ($"{characterName}'s condition is now fine.", ToastType.Success),
            "caution2" => ($"{characterName} is in caution state (2).", ToastType.Warning),
            "caution" => ($"{characterName} is in caution state.", ToastType.Warning),
            "gas" => ($"{characterName} is affected by gas!", ToastType.Warning),
            "danger" => ($"{characterName} is in danger!", ToastType.Error),
            "down" => ($"{characterName} is down!", ToastType.Error),
            "down+gas" => ($"{characterName} is down and affected by gas!", ToastType.Error),
            "" => ("", ToastType.Info),
            _ => ($"{characterName}'s condition: {rawCondition}", ToastType.Error)
        };
    }

    private static (string message, ToastType type) GetStatusToastDetails(string rawStatus, string characterName)
    {
        return rawStatus switch
        {
            "OK" => ($"{characterName}'s status is OK.", ToastType.Success),
            "Dead" => ($"{characterName} is dead!", ToastType.Error),
            "Zombie" => ($"{characterName} is a zombie!", ToastType.Error),
            "Down" => ($"{characterName} is downed!", ToastType.Warning),
            "Gas" => ($"{characterName} is gassed!", ToastType.Warning),
            "Bleed" => ($"{characterName} is bleeding!", ToastType.Warning),
            "" => ("", ToastType.Info),
            _ => ($"{characterName}'s status: {rawStatus}", ToastType.Error)
        };
    }
}