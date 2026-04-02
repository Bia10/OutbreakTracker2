using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Common;
using OutbreakTracker2.Application.SerilogSinks;
using OutbreakTracker2.Application.Services;
using OutbreakTracker2.Application.Services.Atlas;
using OutbreakTracker2.Application.Services.Atlas.Models;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Dispatcher;
using OutbreakTracker2.Application.Services.Embedding;
using OutbreakTracker2.Application.Services.FileLocators;
using OutbreakTracker2.Application.Services.Launcher;
using OutbreakTracker2.Application.Services.Locator;
using OutbreakTracker2.Application.Services.LogStorage;
using OutbreakTracker2.Application.Services.Notifications;
using OutbreakTracker2.Application.Services.PlayerTracking;
using OutbreakTracker2.Application.Services.Toasts;
using OutbreakTracker2.Application.Views.Common;
using OutbreakTracker2.Application.Views.Common.Character;
using OutbreakTracker2.Application.Views.Common.Item;
using OutbreakTracker2.Application.Views.Common.ScenarioImg;
using OutbreakTracker2.Application.Views.Dashboard;
using OutbreakTracker2.Application.Views.Dashboard.ClientAlreadyRunning;
using OutbreakTracker2.Application.Views.Dashboard.ClientNotRunning;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.EmbeddedGame;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameDoors;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameEnemies;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGamePlayer.Factory;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGamePlayers;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.Inventory;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.Inventory.Factory;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.LobbyRoom;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.LobbyRoomPlayer.Factory;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.LobbySlot.Factory;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.LobbySlots;
using OutbreakTracker2.Application.Views.Log;
using OutbreakTracker2.Application.Views.Logging;
using OutbreakTracker2.Application.Views.Map;
using OutbreakTracker2.Application.Views.Map.Canvas;
using OutbreakTracker2.Memory.SafeMemory;
using OutbreakTracker2.Memory.String;
using OutbreakTracker2.PCSX2.EEmem;
using R3;
using Serilog;
using Serilog.Extensions.Logging;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using StringReader = OutbreakTracker2.Memory.String.StringReader;
#if LINUX
using OutbreakTracker2.LinuxInterop;
#endif

namespace OutbreakTracker2.Application;

public sealed partial class App : Avalonia.Application
{
    private static readonly TimeSpan CleanUpTimeout = TimeSpan.FromSeconds(5);

    // Bootstrap logger available before DI is wired — used by the static error handlers.
    // Backed by a minimal Serilog configuration set up in OnFrameworkInitializationCompleted.
    private ILogger<App>? _bootstrapLogger;
    private IServiceProvider? _serviceProvider;

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
            IConfigurationRoot configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            ServiceCollection services = new();
            services.AddSingleton(desktop);
            OutbreakTracker2Views views = ConfigureViews(services);

            _serviceProvider = ConfigureServicesAndLogging(services, configuration);
            _serviceProvider.GetRequiredService<NotificationService>();

            ITextureAtlasService textureAtlasService = _serviceProvider.GetRequiredService<ITextureAtlasService>();
            try
            {
                textureAtlasService.LoadAtlases();
            }
            catch (Exception ex)
            {
                _bootstrapLogger?.LogCritical(ex, "Failed to load texture atlas data. Application cannot start");
                await CleanUpWithTimeoutAsync("texture atlas load failure").ConfigureAwait(true);
                return;
            }

            DataTemplates.Add(new ViewLocator(views));
            desktop.MainWindow = views.CreateView<Views.OutbreakTracker2ViewModel>(_serviceProvider) as Window;

            // Terminate any PCSX2 process whose window surface OT2 is currently embedding
            // so the process is not left running headless when OT2 exits.
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

        // Dispose texture atlases before the container so the service provider is still
        // alive when we resolve ITextureAtlasService.
        if (_serviceProvider?.GetService<ITextureAtlasService>() is TextureAtlasService atlasService)
        {
            foreach (ITextureAtlas atlas in atlasService.GetAllAtlases().Values)
                (atlas as IDisposable)?.Dispose();
        }

        // Several singletons only implement IAsyncDisposable (InGamePlayersViewModel,
        // LobbySlotsViewModel, LobbyRoomViewModel, LogDataStorageService, …).
        if (_serviceProvider is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        else
            (_serviceProvider as IDisposable)?.Dispose();
        _serviceProvider = null;

        await Log.CloseAndFlushAsync().ConfigureAwait(false);

        _bootstrapLogger?.LogInformation("Application exited!");
    }

    private async Task CleanUpWithTimeoutAsync(string operationName)
    {
        Task cleanUpTask = CleanUpAsync();
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

    private static OutbreakTracker2Views ConfigureViews(ServiceCollection services) =>
        new OutbreakTracker2Views()
            // Add main view
            .AddView<Views.OutbreakTracker2View, Views.OutbreakTracker2ViewModel>(services)
            // Add pages
            .AddView<DashboardView, DashboardViewModel>(services)
            .AddView<ClientNotRunningView, ClientNotRunningViewModel>(services)
            .AddView<ClientAlreadyRunningView, ClientAlreadyRunningViewModel>(services)
            .AddView<ClientOverviewView, ClientOverviewViewModel>(services)
            .AddView<InGameScenarioView, InGameScenarioViewModel>(services)
            .AddView<InGamePlayersView, InGamePlayersViewModel>(services)
            .AddView<InGameEnemiesView, InGameEnemiesViewModel>(services)
            .AddView<LobbySlotsView, LobbySlotsViewModel>(services)
            .AddView<LobbyRoomView, LobbyRoomViewModel>(services)
            .AddView<InGameDoorsView, InGameDoorsViewModel>(services)
            .AddView<ItemSlotView, ItemSlotViewModel>(services)
            .AddView<LogView, LogViewModel>(services)
            .AddView<MapView, MapViewModel>(services);

    private static IServiceProvider ConfigureServicesAndLogging(
        IServiceCollection services,
        IConfiguration configuration
    )
    {
        IServiceProvider serviceProvider = ConfigureServices(services, configuration);

        ConfigureSerilog(serviceProvider, configuration);

        return serviceProvider;
    }

    private static void ConfigureSerilog(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        ILogDataStorageService logDataStore = serviceProvider.GetRequiredService<ILogDataStorageService>();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .WriteTo.DataStoreLoggerSink(logDataStore)
            .CreateLogger();

        ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        loggerFactory.AddSerilog(Log.Logger);
    }

    private static ServiceProvider ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(configuration);

        services.AddSingleton<ClipboardService>();
        services.AddSingleton<PageNavigationService>();
        services.AddSingleton<ISukiToastManager, SukiToastManager>();
        services.AddSingleton<ISukiDialogManager, SukiDialogManager>();

        services.AddSingleton<IDispatcherService, DispatcherService>();
        services.AddSingleton<IToastService, ToastService>();
        services.AddSingleton<IPlayerStateTracker, PlayerStateTracker>();
        services.AddSingleton<NotificationService>();

        services.AddSingleton<ILogDataStorageService, LogDataStorageService>();
        services.AddSingleton<LogViewerViewModel>();
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(Log.Logger, dispose: false);
        });

        services.AddSingleton<IPcsx2Locator, Pcsx2Locator>();
        services.AddSingleton<IProcessLocator, ProcessLocator>();
        services.AddSingleton<IProcessLauncher, ProcessLauncher>();

        // Memory reader implementations: platform-specific
        if (OperatingSystem.IsWindows())
        {
            services.AddSingleton<ISafeMemoryReader, SafeMemoryReader>();
            services.AddSingleton<IStringReader, StringReader>();
        }
        else if (OperatingSystem.IsLinux())
        {
#if LINUX
            services.AddSingleton<ISafeMemoryReader, LinuxSafeMemoryReader>();
            services.AddSingleton<IStringReader, LinuxStringReader>();
#else
            throw new PlatformNotSupportedException("Linux reader implementations are only available in Linux builds.");
#endif
        }
        else
        {
            throw new PlatformNotSupportedException("Only Windows and Linux are currently supported.");
        }

        // Window embedding: Windows only for now.
        // Linux support is planned (PCSX2 must be launched by OT2 to inherit permissions),
        // but is not yet implemented.
        if (OperatingSystem.IsWindows())
            services.AddSingleton<IWindowEmbedder, WindowsWindowEmbedder>();
        else
            throw new PlatformNotSupportedException("Window embedding is currently only supported on Windows.");

        services.AddSingleton<EmbeddedGameViewModel>();

        services.AddSingleton<IEEmemMemory, EEmemMemory>();
        services.AddSingleton<IDataManager, DataManager>();
        services.AddSingleton<ITextureAtlasService, TextureAtlasService>();
        services.AddSingleton<Func<Stream, SpriteSheet, ITextureAtlas>>(serviceProvider =>
        {
            return (imageStream, spriteSheet) =>
            {
                ILogger<TextureAtlas> logger = serviceProvider.GetRequiredService<ILogger<TextureAtlas>>();
                return new TextureAtlas(imageStream, spriteSheet, logger);
            };
        });

        services.AddTransient<ImageViewModel>();
        services.AddTransient<CharacterBustViewModel>();
        services.AddTransient<ScenarioImageViewModel>();
        services.AddTransient<LobbyRoomViewModel>();
        services.AddTransient<ItemImageViewModel>();
        services.AddSingleton<MapCanvasViewModel>();

        services.AddTransient<ICharacterBustViewModelFactory, CharacterBustViewModelFactory>();
        services.AddTransient<ILobbyRoomPlayerViewModelFactory, LobbyRoomPlayerViewModelFactory>();
        services.AddTransient<IImageViewModelFactory, ImageViewModelFactory>();
        services.AddTransient<IScenarioImageViewModelFactory, ScenarioImageViewModelFactory>();
        services.AddTransient<ILobbySlotViewModelFactory, LobbySlotViewModelFactory>();
        services.AddTransient<IInGamePlayerViewModelFactory, InGamePlayerViewModelFactory>();
        services.AddTransient<IItemSlotViewModelFactory, ItemSlotViewModelFactory>();
        services.AddTransient<IItemImageViewModelFactory, ItemImageViewModelFactory>();

        services.AddSingleton<LobbySlotsViewModel>();

        ServiceProviderOptions providerOptions = new() { ValidateOnBuild = true, ValidateScopes = true };

        return services.BuildServiceProvider(providerOptions);
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
