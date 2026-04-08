using Bia.LogViewer.Avalonia;
using Bia.LogViewer.Core;
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
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.Entitites;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.Inventory;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.Inventory.Factory;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.LobbyRoom;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.LobbyRoomPlayer.Factory;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.LobbySlot.Factory;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.LobbySlots;
using OutbreakTracker2.Application.Views.GameDock;
using OutbreakTracker2.Application.Views.GameDock.Dockables;
using OutbreakTracker2.Application.Views.Log;
using OutbreakTracker2.Application.Views.Map.Canvas;
using OutbreakTracker2.Memory.SafeMemory;
using OutbreakTracker2.Memory.String;
using OutbreakTracker2.Outbreak.Readers;
using OutbreakTracker2.PCSX2.EEmem;
using Serilog;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using StringReader = OutbreakTracker2.Memory.String.StringReader;
#if LINUX
using OutbreakTracker2.LinuxInterop;
#endif

namespace OutbreakTracker2.Application;

internal static class CompositionRoot
{
    internal static OutbreakTracker2Views ConfigureViews(ServiceCollection services) =>
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
            .AddView<GameDockView, GameDockViewModel>(services);

    internal static IServiceProvider ConfigureServicesAndLogging(
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
        services.AddSingleton<IClipboardService>(sp => sp.GetRequiredService<ClipboardService>());
        services.AddSingleton<PageNavigationService>();
        services.AddSingleton<ISukiToastManager, SukiToastManager>();
        services.AddSingleton<ISukiDialogManager, SukiDialogManager>();

        services.AddSingleton<IDispatcherService, DispatcherService>();
        services.AddSingleton<IToastService, ToastService>();
        services.AddSingleton<IPlayerStateTracker, PlayerStateTracker>();
        services.AddSingleton<INotificationService, NotificationService>();

        services.AddSingleton<ILogDataStorageService, LogDataStorageService>();
        services.AddSingleton<ILogEntrySource>(sp => sp.GetRequiredService<ILogDataStorageService>());
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

        // Game Dock: dockable instances and the factory that wires them into a layout
        services.AddSingleton<GameScreenTool>();
        services.AddSingleton<EnemyListDockTool>();
        services.AddSingleton<MapDockTool>();
        services.AddSingleton<LobbySlotsDockTool>();
        services.AddSingleton<PlayersDockTool>();
        services.AddSingleton<ScenarioInfoDockTool>();
        services.AddSingleton<ScenarioEntitiesViewModel>();
        services.AddSingleton<ScenarioItemsDockTool>();
        services.AddSingleton<ScenarioEnemiesDockTool>();
        services.AddSingleton<ScenarioDoorsDockTool>();
        services.AddSingleton<ScenarioEntityCommands>();
        services.AddSingleton<GameDockFactory>();

        services.AddSingleton<IEEmemMemory, EEmemMemory>();
        services.AddSingleton<IDataManager, DataManager>();
        services.AddSingleton<IGameReaderFactory, GameReaderFactory>();
        services.AddSingleton<IDoorAddressProvider, FileOneDoorAddressProvider>();
        services.AddSingleton<IDoorAddressProvider, FileTwoDoorAddressProvider>();
        services.AddSingleton<ISpriteNameResolver, SpriteNameResolver>();
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
}
