using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Common;
using OutbreakTracker2.Application.Services.Atlas;
using OutbreakTracker2.Application.Services.Launcher;
using OutbreakTracker2.Application.Services.Notifications;
using OutbreakTracker2.Application.Services.Reports;
using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.EmbeddedGame;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameEnemies;
using OutbreakTracker2.Application.Views.GameDock.Dockables;
using R3;
using Serilog;
using Serilog.Extensions.Logging;

namespace OutbreakTracker2.Application;

public sealed partial class App : Avalonia.Application
{
    private static readonly TimeSpan CleanUpTimeout = TimeSpan.FromSeconds(5);
    private readonly Lock _cleanUpGate = new();

    // Bootstrap logger available before DI is wired — used by the static error handlers.
    // Backed by a minimal Serilog configuration set up in OnFrameworkInitializationCompleted.
    private ILogger<App>? _bootstrapLogger;
    private IServiceProvider? _serviceProvider;
    private Task? _cleanUpTask;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        // Configure a minimal bootstrap Serilog logger before DI is available, so the
        // process-level exception handlers below always have somewhere to write.
        // ConfigureSerilog() later replaces Log.Logger with the full configuration from
        // appsettings.json; the SerilogLoggerFactory with no explicit logger forwards to
        // the global Log.Logger dynamically, so _bootstrapLogger automatically picks up the
        // upgraded logger without being recreated.
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Warning()
            .WriteTo.Console()
            .WriteTo.File("logs/log-.txt", rollingInterval: Serilog.RollingInterval.Day)
            .CreateLogger();
        _bootstrapLogger = new SerilogLoggerFactory(dispose: false).CreateLogger<App>();

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        Dispatcher.UIThread.UnhandledException += OnUiUnhandledException;

        ObservableSystem.RegisterUnhandledExceptionHandler(ex =>
            _bootstrapLogger?.LogError(ex, "Unhandled R3 exception")
        );

#if DEBUG
        ObservableTracker.EnableTracking = true;
        ObservableTracker.EnableStackTrace = true;
#endif

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            _ = InitializeApplicationAsync(desktop)
                .ContinueWith(
                    t => _bootstrapLogger?.LogCritical(t.Exception, "Unhandled fault in application initialization"),
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted,
                    TaskScheduler.Default
                );
    }

    private async Task InitializeApplicationAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        try
        {
            string userSettingsPath = AppSettingsFilePaths.GetUserSettingsPath();

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile(userSettingsPath, optional: true, reloadOnChange: true)
                .Build();

            ServiceCollection services = new();
            services.AddSingleton(desktop);
            OutbreakTracker2Views views = CompositionRoot.ConfigureViews(services);

            _serviceProvider = CompositionRoot.ConfigureServicesAndLogging(services, configuration);
            _serviceProvider.GetRequiredService<INotificationService>();
            _serviceProvider.GetRequiredService<IRunReportService>();

            ITextureAtlasService textureAtlasService = _serviceProvider.GetRequiredService<ITextureAtlasService>();
            try
            {
                // Atlas loading must complete synchronously before the window is created.
                // Avalonia's classic desktop lifetime requires MainWindow to be set during
                // the synchronous execution of OnFrameworkInitializationCompleted; any real
                // await point before desktop.MainWindow = ... causes the app to start without
                // a window. Running the async load on the thread pool and blocking here
                // preserves that contract without deadlocking (LoadAtlasesAsync uses
                // ConfigureAwait(false) throughout and never needs to return to the UI thread).
                // ReSharper disable once AsyncApostle.AsyncWait
                Task.Run(async () => await textureAtlasService.LoadAtlasesAsync().ConfigureAwait(false))
                    .GetAwaiter()
                    .GetResult();
            }
            catch (Exception ex)
            {
                _bootstrapLogger?.LogCritical(ex, "Failed to load texture atlas data. Application cannot start");
                await CleanUpWithTimeoutAsync("texture atlas load failure").ConfigureAwait(true);
                return;
            }

            DataTemplates.Add(new ViewLocator(views));
            desktop.MainWindow = views.CreateView<Views.OutbreakTracker2ViewModel>(_serviceProvider) as Window;

            // Pre-warm heavy game-dock singletons during the first idle tick so their
            // constructors run against empty collections (before game attaches).  This
            // eliminates the 1-2 s UI-thread block that would otherwise occur the first
            // time the Game Dock panel is opened with a running game already attached.
            IServiceProvider warmUpProvider = _serviceProvider;
            Dispatcher.UIThread.Post(
                () =>
                {
                    warmUpProvider.GetRequiredService<InGameEnemiesViewModel>();
                    warmUpProvider.GetRequiredService<EntitiesDockViewModel>();
                    warmUpProvider.GetRequiredService<ScenarioItemsDockViewModel>();
                },
                DispatcherPriority.Background
            );

            // Terminate any PCSX2 process whose window surface OT2 is currently embedding
            // so the process is not left running headless when OT2 exits.
            // Unsubscribe first to guarantee exactly-once registration if
            // OnFrameworkInitializationCompleted is called more than once (e.g. hot-reload).
            desktop.ShutdownRequested -= OnShutdownRequested;
            desktop.ShutdownRequested += OnShutdownRequested;

            _bootstrapLogger?.LogInformation("Application initialized successfully!");
            base.OnFrameworkInitializationCompleted();
        }
        catch (Exception ex)
        {
            _bootstrapLogger?.LogCritical(ex, "Application terminated unexpectedly!");
            await CleanUpWithTimeoutAsync("startup exception").ConfigureAwait(true);
        }
    }

    private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        KillEmbeddedProcessIfOwned();
        _ = CleanUpWithTimeoutAsync("application shutdown")
            .ContinueWith(
                t => _bootstrapLogger?.LogError(t.Exception, "Cleanup failed during shutdown"),
                TaskContinuationOptions.OnlyOnFaulted
            );
    }

    private async Task CleanUpAsync()
    {
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
        Dispatcher.UIThread.UnhandledException -= OnUiUnhandledException;

        IServiceProvider? serviceProvider = _serviceProvider;
        _serviceProvider = null;

        // Dispose texture atlases before the container so the service provider is still
        // alive when we resolve ITextureAtlasService.
        if (serviceProvider?.GetService<ITextureAtlasService>() is { } atlasService)
        {
            foreach (ITextureAtlas atlas in atlasService.GetAllAtlases().Values)
                (atlas as IDisposable)?.Dispose();
        }

        // Several singletons only implement IAsyncDisposable (InGamePlayersViewModel,
        // LobbySlotsViewModel, LobbyRoomViewModel, LogDataStorageService, …).
        if (serviceProvider is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        else
            (serviceProvider as IDisposable)?.Dispose();

        await Log.CloseAndFlushAsync().ConfigureAwait(false);

        _bootstrapLogger?.LogInformation("Application exited!");
    }

    private Task GetOrStartCleanUpTask()
    {
        lock (_cleanUpGate)
            return _cleanUpTask ??= CleanUpAsync();
    }

    private async Task CleanUpWithTimeoutAsync(string operationName)
    {
        Task cleanUpTask = GetOrStartCleanUpTask();
        Task completedTask = await Task.WhenAny(cleanUpTask, Task.Delay(CleanUpTimeout)).ConfigureAwait(false);

        if (completedTask != cleanUpTask)
        {
            _bootstrapLogger?.LogWarning(
                "Cleanup timed out after {Timeout} during {Operation}.",
                CleanUpTimeout,
                operationName
            );
            return;
        }

        await cleanUpTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Kills the PCSX2 process if OT2 currently has its window surface embedded.
    /// Must be synchronous (called before async cleanup); uses
    /// <c>Process.Kill()</c> so the process exits immediately without waiting for a graceful close.
    /// </summary>
    private void KillEmbeddedProcessIfOwned()
    {
        if (_serviceProvider is null)
            return;

        EmbeddedGameViewModel? embeddedVm = _serviceProvider.GetService<EmbeddedGameViewModel>();
        IProcessLauncher? launcher = _serviceProvider.GetService<IProcessLauncher>();

        if (
            embeddedVm is not { IsEmbedded: true }
            || launcher?.ClientMonitoredProcess is not { HasExited: false } process
        )
            return;

        try
        {
            _bootstrapLogger?.LogInformation(
                "OT2 shutting down with PCSX2 window embedded (PID {Pid}). Terminating process.",
                process.Id
            );
            process.Kill();
        }
        catch (Exception ex)
        {
            _bootstrapLogger?.LogWarning(ex, "Failed to terminate PCSX2 process on OT2 shutdown");
        }
    }
}
