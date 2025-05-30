using Microsoft.Extensions.Logging;
using OutbreakTracker2.App.Services.Data;
using OutbreakTracker2.App.Services.PlayerTracking;
using OutbreakTracker2.App.Services.Toasts;
using R3;
using System;

namespace OutbreakTracker2.App.Services.Notifications;

public sealed class NotificationService : IDisposable
{
    private readonly IDisposable _playerStateChangeSubscription;
    private readonly IDisposable _dataManagerSubscription;

    public NotificationService(
        IToastService toastService,
        IPlayerStateTracker playerStateTracker,
        IDataManager dataManager,
        ILogger<NotificationService> logger)
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
            .Subscribe(playerStateTracker.PublishPlayerUpdate,
                ex => logger.LogError("Error processing player updates for notifications")
            );
    }

    public void Dispose()
    {
        _playerStateChangeSubscription.Dispose();
        _dataManagerSubscription.Dispose();
    }
}