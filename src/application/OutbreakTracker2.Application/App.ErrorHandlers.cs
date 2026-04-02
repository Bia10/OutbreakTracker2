using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;

namespace OutbreakTracker2.Application;

/// <summary>
/// Global error handler methods for the application.
/// These are registered on process-level event sources in <see cref="App.OnFrameworkInitializationCompleted"/>.
/// </summary>
public sealed partial class App
{
    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = (Exception)e.ExceptionObject;
        _bootstrapLogger?.LogCritical(exception, "Application terminated unexpectedly!");
        _ = CleanUpWithTimeoutAsync("unhandled exception")
            .ContinueWith(
                t => _bootstrapLogger?.LogError(t.Exception, "Cleanup failed after unhandled exception"),
                TaskContinuationOptions.OnlyOnFaulted
            );
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        try
        {
            _bootstrapLogger?.LogCritical(e.Exception, "Unobserved task exception");
        }
        finally
        {
            e.SetObserved();
        }
    }

    private void OnUiUnhandledException(object? sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = HandleFatalUiException(e.Exception);
    }

    internal bool HandleFatalUiException(
        Exception exception,
        IClassicDesktopStyleApplicationLifetime? desktopLifetimeOverride = null
    )
    {
        ArgumentNullException.ThrowIfNull(exception);

        _bootstrapLogger?.LogCritical(exception, "Unhandled UI exception. Requesting application shutdown.");

        var desktop = desktopLifetimeOverride ?? ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;

        if (desktop is null)
        {
            _bootstrapLogger?.LogCritical(
                "Unhandled UI exception occurred without a desktop lifetime. Allowing process to terminate."
            );
            return false;
        }

        bool shutdownRequested = desktop.TryShutdown(-1);
        if (!shutdownRequested)
            _bootstrapLogger?.LogCritical(
                "Unhandled UI exception occurred but graceful shutdown was declined. Allowing process to terminate."
            );

        return shutdownRequested;
    }
}
