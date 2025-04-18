using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using OutbreakTracker2.App.Common;
using OutbreakTracker2.App.Services;
using SukiUI.Dialogs;
using SukiUI.Toasts;

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
            var services = new ServiceCollection();

            services.AddSingleton(desktop);

            OutbreakTracker2Views views = ConfigureViews(services);
            ServiceProvider provider = ConfigureServices(services);

            DataTemplates.Add(new ViewLocator(views));

            desktop.MainWindow = views.CreateView<OutbreakTracker2ViewModel>(provider) as Window;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static OutbreakTracker2Views ConfigureViews(ServiceCollection services)
        => new OutbreakTracker2Views()
            // Add main view
            .AddView<OutbreakTracker2View, OutbreakTracker2ViewModel>(services);
            // Add pages
            //.AddView<DashboardView, DashboardViewModel>(services)
            //.AddView<LoggerView, LoggerViewModel>(services);
            //.AddView<SettingsView, SettingsViewModel>(services)

    private static ServiceProvider ConfigureServices(ServiceCollection services)
    {
        //services.AddSingleton<ClipboardService>();
        services.AddSingleton<PageNavigationService>();
        services.AddSingleton<ISukiToastManager, SukiToastManager>();
        services.AddSingleton<ISukiDialogManager, SukiDialogManager>();

        return services.BuildServiceProvider();
    }
}