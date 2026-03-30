using System;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.PlayerTracking.Rules
{
    public sealed class ConditionTrackingRule(
        string conditionValue,
        Func<string, string, (string message, ToastType type)> toastDetailsFactory
    ) : PlayerStateTrackingRule
    {
        private readonly string _conditionValue = conditionValue.ToLower(
            System.Globalization.CultureInfo.InvariantCulture
        );
        private readonly Func<
            string,
            string,
            (string message, ToastType type)
        > _toastDetailsFactory = toastDetailsFactory;

        public override bool ShouldTrigger(
            DecodedInGamePlayer currentPlayer,
            DecodedInGamePlayer lastKnownPlayerState
        )
        {
            return currentPlayer.Condition.Equals(_conditionValue, StringComparison.Ordinal)
                && !lastKnownPlayerState.Condition.Equals(
                    _conditionValue,
                    StringComparison.Ordinal
                );
        }

        public override PlayerStateChangeEventArgs CreateNotification(
            DecodedInGamePlayer currentPlayer
        )
        {
            var (message, type) = _toastDetailsFactory(currentPlayer.Condition, currentPlayer.Name);
            return new PlayerStateChangeEventArgs(message, "Condition Update", type);
        }
    }
}
