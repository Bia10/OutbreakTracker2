using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
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
using OutbreakTracker2.PCSX2.Client;
using OutbreakTracker2.PCSX2.EEmem;
using R3;
using Serilog;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using System;
using System.IO;
using System.Threading.Tasks;
using StringReader = OutbreakTracker2.Memory.String.StringReader;

namespace OutbreakTracker2.Application;

public class App : Avalonia.Application
{
    private IServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .Build();

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
                    Log.Fatal(ex, "Failed to load texture atlas data. Application cannot start");
                    Environment.Exit(1);
                    return;
                }

                ConfigureExceptionHandling();
                DataTemplates.Add(new ViewLocator(views));
                desktop.MainWindow = views.CreateView<Views.OutbreakTracker2ViewModel>(_serviceProvider) as Window;

                Log.Information("Application initialized successfully!");
            }

            base.OnFrameworkInitializationCompleted();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application failed to initialize");
            // Todo: show msgBox to user
            throw;
        }
    }

    private static void ConfigureExceptionHandling()
    {
        Avalonia.Threading.Dispatcher.UIThread.UnhandledException += (_, e) =>
        {
            Log.Error(e.Exception, "Unhandled exception on UI thread");
            e.Handled = true;
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Log.Error(e.Exception, "Unobserved task exception");
            e.SetObserved();
        };

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                Log.Fatal(ex, "Unhandled exception in application domain (IsTerminating: {IsTerminating})", e.IsTerminating);
            else
                Log.Fatal("Unhandled exception in application domain with unknown exception object: {ExceptionObject} (IsTerminating: {IsTerminating})", e.ExceptionObject, e.IsTerminating);
        };

        ObservableSystem.RegisterUnhandledExceptionHandler(ex =>
        {
            Log.Error(ex, "Unhandled R3 exception");
        });
    }

    private static OutbreakTracker2Views ConfigureViews(ServiceCollection services)
        => new OutbreakTracker2Views()
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
        IConfiguration configuration)
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

    private static ServiceProvider ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration)
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
        services.AddSingleton<ISafeMemoryReader, SafeMemoryReader>();
        services.AddSingleton<IStringReader, StringReader>();
        services.AddSingleton<IGameClientFactory, GameClientFactory>();
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

        ServiceProviderOptions providerOptions = new()
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        };

        return services.BuildServiceProvider(providerOptions);
    }

    public void OnExit()
    {
        (_serviceProvider as IDisposable)?.Dispose();

        if (_serviceProvider?.GetService<ITextureAtlasService>() is TextureAtlasService atlasService)
        {
            foreach (ITextureAtlas atlas in atlasService.GetAllAtlases().Values)
                (atlas as IDisposable)?.Dispose();
        }

        Log.Information("Application exited!");
    }
}