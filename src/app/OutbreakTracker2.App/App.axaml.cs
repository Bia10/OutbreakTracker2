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
            desktop.MainWindow = views.CreateView<OutbreakTracker2ViewModel>(serviceProvider) as Window;

            Log.Information("Application initialized successfully!");
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static OutbreakTracker2Views ConfigureViews(ServiceCollection services)
        => new OutbreakTracker2Views()
            // Add main view
            .AddView<OutbreakTracker2View, OutbreakTracker2ViewModel>(services)
            // Add pages
            .AddView<LogView, LogViewModel>(services);
            //.AddView<DashboardView, DashboardViewModel>(services)
            //.AddView<SettingsView, SettingsViewModel>(services)


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

        services.AddSingleton<ILogDataStorageService, LogDataStorageService>();
        services.AddSingleton<LogViewerViewModel>();
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(Log.Logger, dispose: false);
        });

        var providerOptions = new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        };

        return services.BuildServiceProvider();
    }
}