using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace OutbreakTracker2.App.Services;

public class DispatcherService : IDispatcherService
{
    private readonly ILogger<DispatcherService> _logger;

    public DispatcherService(ILogger<DispatcherService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void PostOnUI(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while executing an action on the UI thread");
            }
        });
    }

    public async Task InvokeOnUIAsync(Action action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                action();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while executing an action on the UI thread");
            }
        });
    }

    public async Task<TResult?> InvokeOnUIAsync<TResult>(Func<TResult> function, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(function);

        return await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return function();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while executing a function on the UI thread");
                return default;
            }
        });
    }
}
