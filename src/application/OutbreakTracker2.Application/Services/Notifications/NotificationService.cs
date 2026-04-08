using OutbreakTracker2.Application.Services.Toasts;
using OutbreakTracker2.Application.Services.Tracking;
using R3;

namespace OutbreakTracker2.Application.Services.Notifications;

public sealed class NotificationService(IToastService toastService, ITrackerRegistry trackerRegistry)
    : INotificationService
{
    private readonly IDisposable _alertSubscription = trackerRegistry.AllAlerts.Subscribe(alert =>
    {
        _ = alert.Level switch
        {
            AlertLevel.Info => toastService.InvokeInfoToastAsync(alert.Message, alert.Title),
            AlertLevel.Warning => toastService.InvokeWarningToastAsync(alert.Message, alert.Title),
            AlertLevel.Error => toastService.InvokeErrorToastAsync(alert.Message, alert.Title),
            AlertLevel.Success => toastService.InvokeSuccessToastAsync(alert.Message, alert.Title),
            _ => toastService.InvokeInfoToastAsync(alert.Message, alert.Title),
        };
    });

    public void Dispose() => _alertSubscription.Dispose();
}
