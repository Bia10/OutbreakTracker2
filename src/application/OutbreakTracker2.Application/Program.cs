using System.Reflection;
using Avalonia;
using Avalonia.Dialogs;

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
                string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
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

        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
            app.UseManagedSystemDialogs();

        return app;
    }
}
