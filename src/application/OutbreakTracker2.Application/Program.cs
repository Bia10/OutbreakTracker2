using Avalonia;
using Avalonia.Dialogs;
using Avalonia.Logging;
using OutbreakTracker2.Application.SerilogSinks;

namespace OutbreakTracker2.Application;

internal static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            // Pre-DI crash — write to a file so crashes are never silently lost
            try
            {
                string version = typeof(App).Assembly.GetName().Version?.ToString() ?? "unknown";
                Directory.CreateDirectory("logs");
                File.AppendAllText("logs/crash.txt", $"[{DateTime.UtcNow:O}] FATAL v{version}: {ex}\n");
            }
            catch (Exception bestEffortEx)
            {
                // Intentionally swallowed: any error here (disk full, permissions denied) must not
                // mask the original exception that caused the crash.
#pragma warning disable ERP022
                _ = bestEffortEx;
#pragma warning restore ERP022
            }

            throw;
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        AppBuilder app = AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont()
#if DEBUG
        .LogToTrace()
#endif
        .UseR3();

        // Write Avalonia-internal diagnostics (binding errors, property coercion,
        // control theme resolution) to a dedicated file. This captures issues that
        // never flow through Serilog because Avalonia uses its own logging pipeline.
        Logger.Sink = new AvaloniaFileSink("logs/avaloniaLog.txt");

        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
            app.UseManagedSystemDialogs();

        return app;
    }
}
