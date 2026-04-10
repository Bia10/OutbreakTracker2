using System.Reflection;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using OutbreakTracker2.Application.Common;

namespace OutbreakTracker2.UnitTests;

public sealed class SettingsDialogRegistrationTests
{
    [Test]
    public async Task AddView_WithRuntimeOnlyViewModel_DoesNotRegisterItInDi()
    {
        ServiceCollection services = [];
        OutbreakTracker2Views views = new();

        views.AddView<FakeDialogView, FakeDialogViewModel>(services, registerViewModel: false);

        using ServiceProvider provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }
        );

        await Assert.That(provider.GetService<FakeDialogViewModel>()).IsNull();

        bool created = views.TryCreateView(new FakeDialogViewModel(), out Control? view);

        await Assert.That(created).IsTrue();
        await Assert.That(view).IsNotNull();
        await Assert.That(view).IsTypeOf<FakeDialogView>();
        await Assert.That(view!.DataContext).IsTypeOf<FakeDialogViewModel>();
    }

    [Test]
    public async Task ConfigureViews_DoesNotRegister_AppSettingsDialogViewModel_InDi()
    {
        ServiceCollection services = [];
        Type compositionRootType = typeof(OutbreakTracker2.Application.App).Assembly.GetType(
            "OutbreakTracker2.Application.CompositionRoot",
            throwOnError: true
        )!;
        MethodInfo configureViews = compositionRootType.GetMethod(
            "ConfigureViews",
            BindingFlags.Static | BindingFlags.NonPublic
        )!;
        Type appSettingsDialogViewModelType = typeof(OutbreakTracker2.Application.App).Assembly.GetType(
            "OutbreakTracker2.Application.Views.Settings.AppSettingsDialogViewModel",
            throwOnError: true
        )!;

        _ = configureViews.Invoke(null, [services]);

        bool hasRegistration = services.Any(descriptor => descriptor.ServiceType == appSettingsDialogViewModelType);

        await Assert.That(hasRegistration).IsFalse();
    }

    private sealed class FakeDialogView : ContentControl;

    private sealed class FakeDialogViewModel : ObservableObject;
}
