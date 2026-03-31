using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.PlayerTracking.Rules
{
    public sealed class StatusTrackingRule(
        string statusValue,
        Func<string, string, (string message, ToastType type)> toastDetailsFactory
    ) : PlayerStateTrackingRule
    {
        private readonly string _statusValue = statusValue;
        private readonly Func<string, string, (string message, ToastType type)> _toastDetailsFactory =
            toastDetailsFactory;

        public override bool ShouldTrigger(DecodedInGamePlayer currentPlayer, DecodedInGamePlayer lastKnownPlayerState)
        {
            return string.Equals(currentPlayer.Status, _statusValue, StringComparison.Ordinal)
                && !string.Equals(lastKnownPlayerState.Status, _statusValue, StringComparison.Ordinal);
        }

        public override PlayerStateChangeEventArgs CreateNotification(DecodedInGamePlayer currentPlayer)
        {
            var (message, type) = _toastDetailsFactory(currentPlayer.Status, currentPlayer.Name);
            return new PlayerStateChangeEventArgs(message, "Status Update", type);
        }
    }
}
