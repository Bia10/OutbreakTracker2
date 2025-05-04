using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OutbreakTracker2.App.Services.Dispatcher;

public class DispatcherService : IDispatcherService
{
    private readonly ILogger<DispatcherService> _logger;

    public DispatcherService(ILogger<DispatcherService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void PostOnUI(Action action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                action();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while executing an action on the UI thread");
            }
        }, DispatcherPriority.Normal);
    }

    public async Task InvokeOnUIAsync(Action action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                action();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while executing an action on the UI thread");
            }
        }, DispatcherPriority.Normal, cancellationToken);
    }

    public async Task<TResult?> InvokeOnUIAsync<TResult>(Func<TResult> function, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(function);
        return await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(function, DispatcherPriority.Normal, cancellationToken);
    }
}
