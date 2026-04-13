using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.Application.Services.Toasts;
using OutbreakTracker2.Application.Services.Tracking;
using R3;

namespace OutbreakTracker2.Application.Services.Notifications;

public sealed class NotificationService : INotificationService
{
    private readonly IDisposable _alertSubscription;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IToastService toastService,
        ITrackerRegistry trackerRegistry,
        IAppSettingsService settingsService,
        ILogger<NotificationService> logger
    )
    {
        _logger = logger;

        _alertSubscription = trackerRegistry.AllAlerts.Subscribe(
            onNext: alert =>
            {
                if (!settingsService.Current.Notifications.EnableToastAlerts)
                    return;

                ObserveToastTask(DispatchToastAsync(toastService, alert), alert);
            },
            onErrorResume: ex => _logger.LogError(ex, "Error in alert notification stream; resuming pipeline"),
            onCompleted: _ => _logger.LogInformation("Alert notification stream completed")
        );
    }

    public void Dispose() => _alertSubscription.Dispose();

    private static Task DispatchToastAsync(IToastService toastService, AlertNotification alert) =>
        alert.Level switch
        {
            AlertLevel.Info => toastService.InvokeInfoToastAsync(alert.Message, alert.Title),
            AlertLevel.Warning => toastService.InvokeWarningToastAsync(alert.Message, alert.Title),
            AlertLevel.Error => toastService.InvokeErrorToastAsync(alert.Message, alert.Title),
            AlertLevel.Success => toastService.InvokeSuccessToastAsync(alert.Message, alert.Title),
            _ => toastService.InvokeInfoToastAsync(alert.Message, alert.Title),
        };

    private void ObserveToastTask(Task toastTask, AlertNotification alert)
    {
        _ = toastTask.ContinueWith(
            task =>
                _logger.LogError(
                    task.Exception,
                    "Toast delivery failed for alert '{AlertTitle}' ({AlertLevel}).",
                    alert.Title,
                    alert.Level
                ),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default
        );
    }
}
