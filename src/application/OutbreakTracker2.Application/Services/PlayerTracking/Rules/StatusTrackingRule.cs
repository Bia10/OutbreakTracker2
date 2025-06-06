using OutbreakTracker2.Outbreak.Models;
using System;

namespace OutbreakTracker2.Application.Services.PlayerTracking.Rules
{
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
            return string.Equals(currentPlayer.Status, _statusValue, StringComparison.Ordinal) &&
                   !string.Equals(lastKnownPlayerState.Status, _statusValue, StringComparison.Ordinal);
        }

        public override PlayerStateChangeEventArgs CreateNotification(DecodedInGamePlayer currentPlayer)
        {
            var (message, type) = _toastDetailsFactory(currentPlayer.Status, currentPlayer.Name);
            return new PlayerStateChangeEventArgs(message, "Status Update", type);
        }
    }
}