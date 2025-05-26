using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.App.Common;
using OutbreakTracker2.App.SerilogSinks;
using OutbreakTracker2.App.Services;
using OutbreakTracker2.App.Services.Data;
using OutbreakTracker2.App.Services.Dispatcher;
using OutbreakTracker2.App.Services.FileLocators;
using OutbreakTracker2.App.Services.LogStorage;
using OutbreakTracker2.App.Services.ProcessLauncher;
using OutbreakTracker2.App.Services.ProcessLocator;
using OutbreakTracker2.App.Services.TextureAtlas;
using OutbreakTracker2.App.Services.TextureAtlas.Models;
using OutbreakTracker2.App.Services.Toasts;
using OutbreakTracker2.App.Views.Common;
using OutbreakTracker2.App.Views.Common.Character;
using OutbreakTracker2.App.Views.Common.Item;
using OutbreakTracker2.App.Views.Common.ScenarioImg;
using OutbreakTracker2.App.Views.Dashboard;
using OutbreakTracker2.App.Views.Dashboard.ClientAlreadyRunning;
using OutbreakTracker2.App.Views.Dashboard.ClientNotRunning;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGameDoors;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGameEnemies;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGamePlayer.Factory;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGamePlayers;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGameScenario;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.Inventory;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.Inventory.Factory;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.LobbyRoom;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.LobbyRoomPlayer.Factory;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.LobbySlot.Factory;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.LobbySlots;
using OutbreakTracker2.App.Views.Log;
using OutbreakTracker2.App.Views.Logging;
using R3;
using Serilog;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using System;
using System.IO;
using System.Threading.Tasks;

namespace OutbreakTracker2.App;

public class App : Application
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
            .AddView<LogView, LogViewModel>(services);

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

        services.AddSingleton<ILogDataStorageService, LogDataStorageService>();
        services.AddSingleton<LogViewerViewModel>();
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(Log.Logger, dispose: false);
        });

        services.AddSingleton<IProcessLocator, ProcessLocator>();
        services.AddSingleton<IDataManager, DataManager>();
        services.AddSingleton<IProcessLauncher, ProcessLauncher>();
        services.AddSingleton<IPcsx2Locator, Pcsx2Locator>();

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