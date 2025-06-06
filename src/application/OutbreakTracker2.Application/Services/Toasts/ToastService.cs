using Avalonia.Controls.Notifications;
using OutbreakTracker2.Application.Services.Dispatcher;
using SukiUI.Toasts;
using System;
using System.Threading.Tasks;

namespace OutbreakTracker2.Application.Services.Toasts;

public class ToastService(ISukiToastManager toastManager, IDispatcherService dispatcher) : IToastService
{
    public Task InvokeInfoToastAsync(string content, string? title = "")
    {
        return dispatcher.InvokeOnUIAsync(() =>
        {
            toastManager.CreateSimpleInfoToast()
                .OfType(NotificationType.Information)
                .WithTitle(string.IsNullOrEmpty(title) ? "Info" : title)
                .WithContent(content)
                .Queue();
        });
    }

    public Task InvokeSuccessToastAsync(string content, string? title = "")
    {
        return dispatcher.InvokeOnUIAsync(() =>
        {
            toastManager.CreateSimpleInfoToast()
                .OfType(NotificationType.Success)
                .WithTitle(string.IsNullOrEmpty(title) ? "Success" : title)
                .WithContent(content)
                .Queue();
        });
    }

    public Task InvokeWarningToastAsync(string content, string? title = "")
    {
        return dispatcher.InvokeOnUIAsync(() =>
        {
            toastManager.CreateSimpleInfoToast()
                .OfType(NotificationType.Warning)
                .WithTitle(string.IsNullOrEmpty(title) ? "Warning" : title)
                .WithContent(content)
                .Queue();
        });
    }

    public Task InvokeErrorToastAsync(string content, string? title = "")
    {
        return dispatcher.InvokeOnUIAsync(() =>
        {
            toastManager.CreateSimpleInfoToast()
                .OfType(NotificationType.Error)
                .WithTitle(string.IsNullOrEmpty(title) ? "Error" : title)
                .WithContent(content)
                .Queue();
        });
    }

    public ISukiToast CreateToast(string title, object content)
    {
        ISukiToast toast = toastManager.CreateToast()
            .WithTitle(title)
            .WithContent(content)
            .Queue();

        return toast;
    }

    public ISukiToast CreateInfoToastWithCancelButton(
        string content,
        object cancelButtonContent,
        Action<ISukiToast> onCanceledAction,
        string? title = "")
    {
        ISukiToast toast = toastManager.CreateToast()
            .OfType(NotificationType.Information)
            .WithTitle(string.IsNullOrEmpty(title) ? "Info" : title)
            .WithContent(content)
            .WithActionButton(cancelButtonContent, onCanceledAction, true)
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
