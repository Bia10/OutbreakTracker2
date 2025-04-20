using SukiUI.Toasts;
using System.Threading.Tasks;
using System;

namespace OutbreakTracker2.App.Services.Toasts;

public interface IToastService
{
    public Task InvokeInfoToastAsync(string content, string? title = "");

    public Task InvokeSuccessToastAsync(string content, string? title = "");

    public Task InvokeErrorToastAsync(string content, string? title = "");

    public Task InvokeWarningToastAsync(string content, string? title = "");

    public ISukiToast CreateToast(string title, object content);

    public ISukiToast CreateInfoToastWithCancelButton(string content, object cancelButtonContent,
        Action<ISukiToast> onCanceledAction, string? title = "");

    public Task DismissToastAsync(ISukiToast toast);
}
