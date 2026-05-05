using System.Reflection;
using OutbreakTracker2.Application.Services.Settings;
using R3;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace OutbreakTracker2.UnitTests;

public sealed class AppSettingsDialogViewModelTests
{
    [Test]
    public async Task Constructor_NormalizesNullLobbyFilters_WithoutCrashing()
    {
        using FakeAppSettingsService settingsService = new(
            new OutbreakTrackerSettings
            {
                AlertRules = new AlertRuleSettings
                {
                    Lobby = new LobbyAlertRuleSettings
                    {
                        NameMatchCreated = true,
                        NameMatchFilter = null!,
                        ScenarioMatchCreated = true,
                        ScenarioMatchFilter = null!,
                    },
                },
            }
        );

        Type viewModelType = typeof(OutbreakTracker2.Application.App).Assembly.GetType(
            "OutbreakTracker2.Application.Views.Settings.AppSettingsDialogViewModel",
            throwOnError: true
        )!;

        object viewModel = Activator.CreateInstance(
            viewModelType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: [settingsService, CreateProxy<ISukiToastManager>(), CreateProxy<ISukiDialog>()],
            culture: null
        )!;

        string? lobbyNameMatchFilter = (string?)viewModelType.GetProperty("LobbyNameMatchFilter")!.GetValue(viewModel);
        string? lobbyScenarioMatchFilter = (string?)
            viewModelType.GetProperty("LobbyScenarioMatchFilter")!.GetValue(viewModel);
        string? validationMessage = (string?)viewModelType.GetProperty("ValidationMessage")!.GetValue(viewModel);

        await Assert.That(lobbyNameMatchFilter).IsEqualTo(string.Empty);
        await Assert.That(lobbyScenarioMatchFilter).IsEqualTo(string.Empty);
        await Assert
            .That(validationMessage)
            .IsEqualTo(
                "OutbreakTracker:AlertRules:Lobby:NameMatchFilter cannot be empty when NameMatchCreated is enabled."
            );
    }

    [Test]
    public async Task Constructor_NormalizesLegacyScenarioFilter_ToExactScenarioSelection()
    {
        using FakeAppSettingsService settingsService = new(
            new OutbreakTrackerSettings
            {
                AlertRules = new AlertRuleSettings
                {
                    Lobby = new LobbyAlertRuleSettings { ScenarioMatchCreated = true, ScenarioMatchFilter = "Wild" },
                },
            }
        );

        Type viewModelType = typeof(OutbreakTracker2.Application.App).Assembly.GetType(
            "OutbreakTracker2.Application.Views.Settings.AppSettingsDialogViewModel",
            throwOnError: true
        )!;

        object viewModel = Activator.CreateInstance(
            viewModelType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: [settingsService, CreateProxy<ISukiToastManager>(), CreateProxy<ISukiDialog>()],
            culture: null
        )!;

        string? lobbyScenarioMatchFilter = (string?)
            viewModelType.GetProperty("LobbyScenarioMatchFilter")!.GetValue(viewModel);
        IReadOnlyList<string> lobbyScenarioOptions =
            (IReadOnlyList<string>)viewModelType.GetProperty("LobbyScenarioOptions")!.GetValue(viewModel)!;

        await Assert.That(lobbyScenarioMatchFilter).IsEqualTo("Wild things");
        await Assert.That(lobbyScenarioOptions.Contains("Wild things")).IsTrue();
    }

    [Test]
    public async Task BuildSettings_PreservesDisplayToggles()
    {
        using FakeAppSettingsService settingsService = new(
            new OutbreakTrackerSettings
            {
                Display = new DisplaySettings
                {
                    ShowGameplayUiDuringTransitions = false,
                    EntitiesDock = new EntitiesDockSettings { OnlyShowCurrentPlayerRoom = false },
                    ScenarioItemsDock = new ScenarioItemsDockSettings
                    {
                        OnlyShowCurrentPlayerRoom = false,
                        ProjectAllOntoMap = false,
                    },
                },
            }
        );

        Type viewModelType = typeof(OutbreakTracker2.Application.App).Assembly.GetType(
            "OutbreakTracker2.Application.Views.Settings.AppSettingsDialogViewModel",
            throwOnError: true
        )!;

        object viewModel = Activator.CreateInstance(
            viewModelType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: [settingsService, CreateProxy<ISukiToastManager>(), CreateProxy<ISukiDialog>()],
            culture: null
        )!;

        bool loadedValue = (bool)
            viewModelType.GetProperty("EntitiesDockOnlyShowCurrentPlayerRoom")!.GetValue(viewModel)!;
        bool loadedItemsValue = (bool)
            viewModelType.GetProperty("ScenarioItemsDockOnlyShowCurrentPlayerRoom")!.GetValue(viewModel)!;
        bool loadedItemsProjectionValue = (bool)
            viewModelType.GetProperty("ScenarioItemsProjectAllOntoMap")!.GetValue(viewModel)!;
        bool loadedTransitionValue = (bool)
            viewModelType.GetProperty("ShowGameplayUiDuringTransitions")!.GetValue(viewModel)!;
        viewModelType.GetProperty("EntitiesDockOnlyShowCurrentPlayerRoom")!.SetValue(viewModel, true);
        viewModelType.GetProperty("ScenarioItemsDockOnlyShowCurrentPlayerRoom")!.SetValue(viewModel, true);
        viewModelType.GetProperty("ScenarioItemsProjectAllOntoMap")!.SetValue(viewModel, true);
        viewModelType.GetProperty("ShowGameplayUiDuringTransitions")!.SetValue(viewModel, true);

        MethodInfo buildSettingsMethod = viewModelType.GetMethod(
            "BuildSettings",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;
        OutbreakTrackerSettings builtSettings = (OutbreakTrackerSettings)buildSettingsMethod.Invoke(viewModel, null)!;

        await Assert.That(loadedValue).IsFalse();
        await Assert.That(loadedItemsValue).IsFalse();
        await Assert.That(loadedItemsProjectionValue).IsFalse();
        await Assert.That(loadedTransitionValue).IsFalse();
        await Assert.That(builtSettings.Display.ShowGameplayUiDuringTransitions).IsTrue();
        await Assert.That(builtSettings.Display.EntitiesDock.OnlyShowCurrentPlayerRoom).IsTrue();
        await Assert.That(builtSettings.Display.ScenarioItemsDock.OnlyShowCurrentPlayerRoom).IsTrue();
        await Assert.That(builtSettings.Display.ScenarioItemsDock.ProjectAllOntoMap).IsTrue();
    }

    private static T CreateProxy<T>()
        where T : class => DispatchProxy.Create<T, DefaultValueProxy>();

    private class DefaultValueProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod is null || targetMethod.ReturnType == typeof(void))
                return null;

            if (targetMethod.ReturnType == typeof(string))
                return string.Empty;

            return targetMethod.ReturnType.IsValueType ? Activator.CreateInstance(targetMethod.ReturnType) : null;
        }
    }

    private sealed class FakeAppSettingsService : IAppSettingsService
    {
        private readonly ReactiveProperty<OutbreakTrackerSettings> _settings;

        public FakeAppSettingsService(OutbreakTrackerSettings settings)
        {
            _settings = new ReactiveProperty<OutbreakTrackerSettings>(settings);
        }

        public string UserSettingsPath { get; } =
            Path.Combine(Path.GetTempPath(), "outbreaktracker2-test-settings.json");

        public OutbreakTrackerSettings Current => _settings.Value;

        public Observable<OutbreakTrackerSettings> SettingsObservable => _settings;

        public ValueTask SaveAsync(OutbreakTrackerSettings settings, CancellationToken cancellationToken = default)
        {
            _settings.Value = settings;
            return ValueTask.CompletedTask;
        }

        public ValueTask ExportAsync(Stream destination, CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;

        public ValueTask<OutbreakTrackerSettings> ImportAsync(
            Stream source,
            CancellationToken cancellationToken = default
        ) => ValueTask.FromResult(Current);

        public ValueTask<OutbreakTrackerSettings> ResetToDefaultsAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Current);

        public void Dispose() => _settings.Dispose();
    }
}
