using OutbreakTracker2.App.Services.PlayerTracking;
using OutbreakTracker2.App.Services.Toasts;
using R3;
using System;

namespace OutbreakTracker2.App.Services.Notifications;

public sealed class NotificationService : IDisposable
{
    private readonly IDisposable _subscription;

    public NotificationService(IToastService toastService, IPlayerStateTracker playerStateTracker)
    {
        _subscription = playerStateTracker.PlayerStateChanges
            .Subscribe(playerStateChangedEvent =>
            {
                _ = playerStateChangedEvent.Type switch
                {
                    ToastType.Info => toastService.InvokeInfoToastAsync(playerStateChangedEvent.Message, playerStateChangedEvent.Title),
                    ToastType.Warning => toastService.InvokeWarningToastAsync(playerStateChangedEvent.Message, playerStateChangedEvent.Title),
                    ToastType.Error => toastService.InvokeErrorToastAsync(playerStateChangedEvent.Message, playerStateChangedEvent.Title),
                    ToastType.Success => toastService.InvokeSuccessToastAsync(playerStateChangedEvent.Message, playerStateChangedEvent.Title),
                    _ => toastService.InvokeInfoToastAsync(playerStateChangedEvent.Message, playerStateChangedEvent.Title)
                };
            });
    }

    public void Dispose()
    {
        _subscription.Dispose();
    }
}