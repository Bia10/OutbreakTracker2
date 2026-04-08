using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.PlayerTracking;
using OutbreakTracker2.Application.Services.Toasts;
using R3;

namespace OutbreakTracker2.Application.Services.Notifications;

public sealed class NotificationService(
    IToastService toastService,
    IPlayerStateTracker playerStateTracker,
    IDataManager dataManager
) : INotificationService
{
    private readonly IDisposable _playerStateChangeSubscription = playerStateTracker.PlayerStateChanges.Subscribe(
        playerStateChangedEvent =>
        {
            _ = playerStateChangedEvent.Type switch
            {
                ToastType.Info => toastService.InvokeInfoToastAsync(
                    playerStateChangedEvent.Message,
                    playerStateChangedEvent.Title
                ),
                ToastType.Warning => toastService.InvokeWarningToastAsync(
                    playerStateChangedEvent.Message,
                    playerStateChangedEvent.Title
                ),
                ToastType.Error => toastService.InvokeErrorToastAsync(
                    playerStateChangedEvent.Message,
                    playerStateChangedEvent.Title
                ),
                ToastType.Success => toastService.InvokeSuccessToastAsync(
                    playerStateChangedEvent.Message,
                    playerStateChangedEvent.Title
                ),
                _ => toastService.InvokeInfoToastAsync(playerStateChangedEvent.Message, playerStateChangedEvent.Title),
            };
        }
    );
    private readonly IDisposable _dataManagerSubscription = dataManager
        .InGamePlayersObservable.SelectMany(players => players.ToObservable())
        .Subscribe(playerStateTracker.PublishPlayerUpdate);

    public void Dispose()
    {
        _playerStateChangeSubscription.Dispose();
        _dataManagerSubscription.Dispose();
    }
}
