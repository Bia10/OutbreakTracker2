using OutbreakTracker2.Outbreak.Models;
using System;

namespace OutbreakTracker2.App.Services.PlayerTracking;

public abstract class PlayerStateTrackingRule
{
    public abstract bool ShouldTrigger(DecodedInGamePlayer currentPlayer, DecodedInGamePlayer lastKnownPlayerState);
    public abstract PlayerStateChangeEventArgs CreateNotification(DecodedInGamePlayer currentPlayer);
}

public sealed class ConditionTrackingRule : PlayerStateTrackingRule
{
    private readonly string _conditionValue;
    private readonly Func<string, string, (string message, ToastType type)> _toastDetailsFactory;

    public ConditionTrackingRule(string conditionValue, Func<string, string, (string message, ToastType type)> toastDetailsFactory)
    {
        _conditionValue = conditionValue.ToLower();
        _toastDetailsFactory = toastDetailsFactory;
    }

    public override bool ShouldTrigger(DecodedInGamePlayer currentPlayer, DecodedInGamePlayer lastKnownPlayerState)
    {
        return currentPlayer.Condition.Equals(_conditionValue, StringComparison.Ordinal) &&
               !lastKnownPlayerState.Condition.Equals(_conditionValue, StringComparison.Ordinal);
    }

    public override PlayerStateChangeEventArgs CreateNotification(DecodedInGamePlayer currentPlayer)
    {
        var (message, type) = _toastDetailsFactory(currentPlayer.Condition, currentPlayer.Name);
        return new PlayerStateChangeEventArgs(message, "Condition Update", type);
    }
}

public sealed class StatusTrackingRule : PlayerStateTrackingRule
{
    private readonly string _statusValue;
    private readonly Func<string, string, (string message, ToastType type)> _toastDetailsFactory;

    public StatusTrackingRule(string statusValue, Func<string, string, (string message, ToastType type)> toastDetailsFactory)
    {
        _statusValue = statusValue;
        _toastDetailsFactory = toastDetailsFactory;
    }

    public override bool ShouldTrigger(DecodedInGamePlayer currentPlayer, DecodedInGamePlayer lastKnownPlayerState)
    {
        return currentPlayer.Status == _statusValue &&
               lastKnownPlayerState.Status != _statusValue;
    }

    public override PlayerStateChangeEventArgs CreateNotification(DecodedInGamePlayer currentPlayer)
    {
        var (message, type) = _toastDetailsFactory(currentPlayer.Status, currentPlayer.Name);
        return new PlayerStateChangeEventArgs(message, "Status Update", type);
    }
}

public sealed class CustomTrackingRule : PlayerStateTrackingRule
{
    private readonly Func<DecodedInGamePlayer, DecodedInGamePlayer, bool> _condition;
    private readonly Func<DecodedInGamePlayer, PlayerStateChangeEventArgs> _notificationFactory;

    public CustomTrackingRule(Func<DecodedInGamePlayer, DecodedInGamePlayer, bool> condition, Func<DecodedInGamePlayer, PlayerStateChangeEventArgs> notificationFactory)
    {
        _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        _notificationFactory = notificationFactory ?? throw new ArgumentNullException(nameof(notificationFactory));
    }

    public override bool ShouldTrigger(DecodedInGamePlayer currentPlayer, DecodedInGamePlayer lastKnownPlayerState)
    {
        return _condition(currentPlayer, lastKnownPlayerState);
    }

    public override PlayerStateChangeEventArgs CreateNotification(DecodedInGamePlayer currentPlayer)
    {
        return _notificationFactory(currentPlayer);
    }
}