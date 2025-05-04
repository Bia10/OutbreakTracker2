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
using OutbreakTracker2.App.Services.Toasts;
using OutbreakTracker2.App.Views.Dashboard;
using OutbreakTracker2.App.Views.Dashboard.ClientAlreadyRunning;
using OutbreakTracker2.App.Views.Dashboard.ClientNotRunning;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.Debug;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGameEnemies;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGamePlayers;
using OutbreakTracker2.App.Views.Log;
using OutbreakTracker2.App.Views.Logging;
using Serilog;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using System;

namespace OutbreakTracker2.App;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var services = new ServiceCollection();

            services.AddSingleton(desktop);

            OutbreakTracker2Views views = ConfigureViews(services);
            DataTemplates.Add(new ViewLocator(views));

            IServiceProvider serviceProvider = ConfigureServicesAndLogging(services, configuration);
            desktop.MainWindow = views.CreateView<Views.OutbreakTracker2ViewModel>(serviceProvider) as Window;

            Log.Information("Application initialized successfully!");
        }

        base.OnFrameworkInitializationCompleted();
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
            .AddView<DebugView, DebugViewModel>(services)
            .AddView<InGamePlayersView, InGamePlayersViewModel>(services)
            .AddView<InGameEnemiesView, InGameEnemiesViewModel>(services)
            .AddView<LogView, LogViewModel>(services);

    private static IServiceProvider ConfigureServicesAndLogging(IServiceCollection services, IConfiguration configuration)
    {
        IServiceProvider serviceProvider = ConfigureServices(services, configuration);

        ConfigureSerilog(serviceProvider, configuration);

        return serviceProvider;
    }

    private static void ConfigureSerilog(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        ILogDataStorageService logDataStore = serviceProvider.GetRequiredService<ILogDataStorageService>();

        Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(configuration)
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
        services.AddSingleton<IPCSX2Locator, PCSX2Locator>();

        var providerOptions = new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        };

        return services.BuildServiceProvider(providerOptions);
    }
}