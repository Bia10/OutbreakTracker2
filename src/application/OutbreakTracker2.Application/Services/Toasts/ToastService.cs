using Avalonia.Controls.Notifications;
using OutbreakTracker2.Application.Services.Dispatcher;
using SukiUI.Toasts;

namespace OutbreakTracker2.Application.Services.Toasts;

public sealed class ToastService(ISukiToastManager toastManager, IDispatcherService dispatcher) : IToastService
{
    private Task InvokeToastAsync(NotificationType type, string content, string? title, string defaultTitle)
    {
        return dispatcher.InvokeOnUIAsync(() =>
        {
            toastManager
                .CreateSimpleInfoToast()
                .OfType(type)
                .WithTitle(string.IsNullOrEmpty(title) ? defaultTitle : title)
                .WithContent(content)
                .Queue();
        });
    }

    public Task InvokeInfoToastAsync(string content, string? title = "") =>
        InvokeToastAsync(NotificationType.Information, content, title, "Info");

    public Task InvokeSuccessToastAsync(string content, string? title = "") =>
        InvokeToastAsync(NotificationType.Success, content, title, "Success");

    public Task InvokeWarningToastAsync(string content, string? title = "") =>
        InvokeToastAsync(NotificationType.Warning, content, title, "Warning");

    public Task InvokeErrorToastAsync(string content, string? title = "") =>
        InvokeToastAsync(NotificationType.Error, content, title, "Error");

    public ISukiToast CreateToast(string title, object content)
    {
        ISukiToast toast = toastManager.CreateToast().WithTitle(title).WithContent(content).Queue();

        return toast;
    }

    public ISukiToast CreateInfoToastWithCancelButton(
        string content,
        object cancelButtonContent,
        Action<ISukiToast> onCanceledAction,
        string? title = ""
    )
    {
        ISukiToast toast = toastManager
            .CreateToast()
            .OfType(NotificationType.Information)
            .WithTitle(string.IsNullOrEmpty(title) ? "Info" : title)
            .WithContent(content)
            .WithActionButton(cancelButtonContent, onCanceledAction, dismissOnClick: true)
            .Queue();

        return toast;
    }

    public Task DismissToastAsync(ISukiToast toast)
    {
        return dispatcher.InvokeOnUIAsync(() =>
        {
            toastManager.Dismiss(toast);
        });
    }
}
