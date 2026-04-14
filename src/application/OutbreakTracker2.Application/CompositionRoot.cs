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
using OutbreakTracker2.Application.Services.Reports;
using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.Application.Services.Toasts;
using OutbreakTracker2.Application.Services.Tracking;
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
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.Entities;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.FileOne;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.FileTwo;
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
using OutbreakTracker2.Application.Views.Settings;
using OutbreakTracker2.Memory.SafeMemory;
using OutbreakTracker2.Memory.String;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Readers;
using OutbreakTracker2.PCSX2.Client;
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
    internal static OutbreakTracker2Views ConfigureViews(IServiceCollection services) =>
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
            .AddView<GameDockView, GameDockViewModel>(services)
            .AddView<AppSettingsDialogView, AppSettingsDialogViewModel>(services, registerViewModel: false)
            .AddView<FlashbackView, FlashbackViewModel>(services, registerViewModel: false);

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
        // IMPORTANT: ILogDataStorageService must be resolved here — before AddSerilog — so its
        // background task is running when the DataStoreLoggerSink starts writing entries.
        // Do NOT reorder: the sink references the store by interface, not by DI resolution order.
        ILogDataStorageService logDataStore = serviceProvider.GetRequiredService<ILogDataStorageService>();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .WriteTo.DataStoreLoggerSink(logDataStore)
            .CreateLogger();

        // Adds the fully configured Serilog logger to the already-built ILoggerFactory.
        // Must run after Log.Logger is assigned above so ILogger<T> instances created after this
        // point forward to the correct Serilog pipeline.
        ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        loggerFactory.AddSerilog(Log.Logger);
    }

    private static ServiceProvider ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(configuration);

        AddCoreServices(services, configuration);
        AddPlatformServices(services);
        AddDockAndViewServices(services);
        AddDataServices(services, configuration);

        ServiceProviderOptions providerOptions = new() { ValidateOnBuild = true, ValidateScopes = true };

        return services.BuildServiceProvider(providerOptions);
    }

    private static void AddCoreServices(IServiceCollection services, IConfiguration configuration)
    {
        RunReportOptions runReportOptions =
            configuration.GetSection(RunReportOptions.SectionName).Get<RunReportOptions>() ?? new RunReportOptions();

        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<PageNavigationService>();
        services.AddSingleton<ISukiToastManager, SukiToastManager>();
        services.AddSingleton<ISukiDialogManager, SukiDialogManager>();

        services.AddSingleton<IDispatcherService, DispatcherService>();
        services.AddSingleton<IToastService, ToastService>();
        services.AddSingleton<ISettingsSerializer, SettingsJsonSerializer>();
        services.AddSingleton<ISettingsValidator, SettingsValidator>();
        services.AddSingleton<ISettingsPersistence>(_ => new FileSettingsPersistence(
            AppSettingsFilePaths.GetUserSettingsPath()
        ));
        services.AddSingleton<IAppSettingsService>(serviceProvider => new AppSettingsService(
            serviceProvider.GetRequiredService<IConfiguration>(),
            serviceProvider.GetRequiredService<ILogger<AppSettingsService>>(),
            serviceProvider.GetRequiredService<ISettingsSerializer>(),
            serviceProvider.GetRequiredService<ISettingsValidator>(),
            serviceProvider.GetRequiredService<ISettingsPersistence>()
        ));
        services.AddSingleton(runReportOptions);
        services.AddSingleton<IEntityTrackerFactory, EntityTrackerFactory>();
        services.AddSingleton<IAlertRuleProvider<DecodedEnemy>, EnemyAlertRulesProvider>();
        services.AddSingleton<IAlertRuleProvider<DecodedDoor>, DoorAlertRulesProvider>();
        services.AddSingleton<IAlertRuleProvider<DecodedInGamePlayer>, PlayerAlertRulesProvider>();
        services.AddSingleton<IAlertRuleProvider<DecodedLobbySlot>, LobbySlotAlertRulesProvider>();
        services.AddSingleton<ITrackerRegistry, TrackerRegistry>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IRunReportService, RunReportService>();
        services.AddSingleton<MarkdownRunReportWriter>();
        services.AddSingleton<HtmlRunReportWriter>();
        services.AddSingleton<CsvRunReportWriter>();
        services.AddSingleton<IRunReportWriter, CompositeRunReportWriter>();

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
        services.AddSingleton<IGameClientFactory, GameClientFactory>();
        services.AddSingleton<IProcessLauncher, ProcessLauncher>();
        services.AddSingleton<IGameClientConnectionService, GameClientConnectionService>();
    }

    private static void AddPlatformServices(IServiceCollection services)
    {
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
    }

    private static void AddDockAndViewServices(IServiceCollection services)
    {
        // Game Dock: dockable instances and the factory that wires them into a layout
        services.AddSingleton<GameScreenDockTool>();
        services.AddSingleton<EntitiesDockViewModel>();
        services.AddSingleton<EntitiesDockTool>();
        services.AddSingleton<MapDockTool>();
        services.AddSingleton<PlayersDockTool>();
        services.AddSingleton<ScenarioInfoDockTool>();
        services.AddSingleton<ScenarioItemsViewModel>();
        services.AddSingleton<ScenarioEnemiesViewModel>();
        services.AddSingleton<ScenarioItemsDockViewModel>();
        services.AddSingleton<ScenarioItemsDockTool>();
        services.AddSingleton<ScenarioEnemiesDockTool>();
        services.AddSingleton<ScenarioDoorsDockTool>();
        services.AddSingleton<ScenarioEntityCommands>();
        AddScenarioSpecificViewModels(services);
        services.AddSingleton<DockToolSet>();
        services.AddSingleton<GameDockFactory>();

        services.AddSingleton<MapCanvasViewModel>();
        services.AddSingleton<IEnemyCardCollectionSource>(sp => sp.GetRequiredService<InGameEnemiesViewModel>());

        services.AddTransient<ICharacterBustViewModelFactory, CharacterBustViewModelFactory>();
        services.AddTransient<ILobbyRoomPlayerViewModelFactory, LobbyRoomPlayerViewModelFactory>();
        services.AddTransient<IImageViewModelFactory, ImageViewModelFactory>();
        services.AddTransient<IScenarioImageViewModelFactory, ScenarioImageViewModelFactory>();
        services.AddTransient<ILobbySlotViewModelFactory, LobbySlotViewModelFactory>();
        services.AddTransient<IInGamePlayerViewModelFactory, InGamePlayerViewModelFactory>();
        services.AddTransient<IInGamePlayerSubViewModelFactory, InGamePlayerSubViewModelFactory>();
        services.AddTransient<IItemSlotViewModelFactory, ItemSlotViewModelFactory>();
        services.AddTransient<IItemImageViewModelFactory, ItemImageViewModelFactory>();

        services.AddSingleton<LobbySlotsViewModel>();
    }

    private static void AddScenarioSpecificViewModels(IServiceCollection services)
    {
        services.AddSingleton<IScenarioSpecificViewModel, DesperateTimesViewModel>();
        services.AddSingleton<IScenarioSpecificViewModel, EndOfTheRoadViewModel>();
        services.AddSingleton<IScenarioSpecificViewModel, FlashbackViewModel>();
        services.AddSingleton<IScenarioSpecificViewModel, UnderbellyViewModel>();
        services.AddSingleton<IScenarioSpecificViewModel, WildThingsViewModel>();
        services.AddSingleton<IScenarioSpecificViewModel, BelowFreezingPointViewModel>();
        services.AddSingleton<IScenarioSpecificViewModel, DecisionsDecisionsViewModel>();
        services.AddSingleton<IScenarioSpecificViewModel, HellfireViewModel>();
        services.AddSingleton<IScenarioSpecificViewModel, TheHiveViewModel>();
        services.AddSingleton(sp => new ScenarioViewModelRouter(
            sp.GetRequiredService<IEnumerable<IScenarioSpecificViewModel>>()
        ));
    }

    internal static TextureAtlasOptions GetTextureAtlasOptions(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        IConfigurationSection atlasesSection = configuration
            .GetSection(TextureAtlasOptions.SectionName)
            .GetSection(nameof(TextureAtlasOptions.Atlases));

        List<TextureAtlasDefinition> atlases = [];
        foreach (IConfigurationSection atlasSection in atlasesSection.GetChildren())
        {
            atlases.Add(
                new TextureAtlasDefinition
                {
                    Name = atlasSection[nameof(TextureAtlasDefinition.Name)] ?? string.Empty,
                    JsonPath = atlasSection[nameof(TextureAtlasDefinition.JsonPath)] ?? string.Empty,
                    ImagePath = atlasSection[nameof(TextureAtlasDefinition.ImagePath)] ?? string.Empty,
                }
            );
        }

        return new TextureAtlasOptions { Atlases = atlases };
    }

    private static void AddDataServices(IServiceCollection services, IConfiguration configuration)
    {
        TextureAtlasOptions textureAtlasOptions = GetTextureAtlasOptions(configuration);

        services.AddSingleton<IEEmemMemory, EEmemMemory>();
        services.AddSingleton<DataManagerOptions>(sp =>
        {
            DataManagerSettings userSettings = sp.GetRequiredService<IAppSettingsService>().Current.DataManager;
            return new DataManagerOptions
            {
                FastUpdateIntervalMs = userSettings.FastUpdateIntervalMs,
                SlowUpdateIntervalMs = userSettings.SlowUpdateIntervalMs,
            };
        });
        services.AddSingleton<IDataManager, DataManager>();
        services.AddSingleton<IDataObservableSource>(sp => sp.GetRequiredService<IDataManager>());
        services.AddSingleton<IDataSnapshot>(sp => sp.GetRequiredService<IDataManager>());
        services.AddSingleton<ICurrentScenarioState>(sp =>
            (ICurrentScenarioState)sp.GetRequiredService<IDataManager>()
        );
        services.AddSingleton<IGameReaderFactory, GameReaderFactory>();
        services.AddSingleton<IDoorAddressProvider, FileOneDoorAddressProvider>();
        services.AddSingleton<IDoorAddressProvider, FileTwoDoorAddressProvider>();
        services.AddSingleton<ISpriteNameResolver, SpriteNameResolver>();
        services.AddSingleton(textureAtlasOptions);
        services.AddSingleton<ITextureAtlasService, TextureAtlasService>();
        services.AddSingleton<Func<Stream, SpriteSheet, ITextureAtlas>>(serviceProvider =>
        {
            ILogger<TextureAtlas> logger = serviceProvider.GetRequiredService<ILogger<TextureAtlas>>();
            return (imageStream, spriteSheet) => new TextureAtlas(imageStream, spriteSheet, logger);
        });
    }
}
