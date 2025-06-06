using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.PlayerTracking;
using OutbreakTracker2.Application.Services.Toasts;
using R3;
using System;

namespace OutbreakTracker2.Application.Services.Notifications;

public sealed class NotificationService : IDisposable
{
    private readonly IDisposable _playerStateChangeSubscription;
    private readonly IDisposable _dataManagerSubscription;

    public NotificationService(
        IToastService toastService,
        IPlayerStateTracker playerStateTracker,
        IDataManager dataManager)
    {
        _playerStateChangeSubscription = playerStateTracker.PlayerStateChanges
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

        _dataManagerSubscription = dataManager.InGamePlayersObservable
            .SelectMany(players => players.ToObservable())
            .Subscribe(playerStateTracker.PublishPlayerUpdate);
    }

    public void Dispose()
    {
        _playerStateChangeSubscription.Dispose();
        _dataManagerSubscription.Dispose();
    }
}