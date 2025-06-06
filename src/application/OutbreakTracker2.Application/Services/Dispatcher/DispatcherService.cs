using Avalonia.Threading;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OutbreakTracker2.Application.Services.Dispatcher;

public sealed class DispatcherService : IDispatcherService
{
    public bool IsOnUIThread()
        => Avalonia.Threading.Dispatcher.UIThread.CheckAccess();

    public void PostOnUI(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        Avalonia.Threading.Dispatcher.UIThread.Post(action, DispatcherPriority.Normal);
    }

    public async Task InvokeOnUIAsync(Action action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(action, DispatcherPriority.Normal, cancellationToken);
    }

    public async Task<TResult?> InvokeOnUIAsync<TResult>(Func<TResult> function, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(function);

        return await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(function, DispatcherPriority.Normal, cancellationToken);
    }
}
