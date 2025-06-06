using OutbreakTracker2.Outbreak.Models;
using System;

namespace OutbreakTracker2.Application.Services.PlayerTracking.Rules
{
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
}