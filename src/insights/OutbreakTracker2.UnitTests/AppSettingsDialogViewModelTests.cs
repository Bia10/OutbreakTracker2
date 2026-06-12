using System.Diagnostics;
using System.Reflection;
using System.Windows.Input;
using MemoryWatcher;
using MemoryWatcher.Remote;
using OutbreakTracker2.Application.Services.Capabilities;
using OutbreakTracker2.Application.Services.Launcher;
using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.MemoryWatcherIntegration;
using OutbreakTracker2.PCSX2.Client;
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

        object viewModel = CreateViewModel(viewModelType, settingsService);

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

        object viewModel = CreateViewModel(viewModelType, settingsService);

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

        object viewModel = CreateViewModel(viewModelType, settingsService);

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

    [Test]
    public async Task BuildSettings_PreservesMemoryWatcherAndReportSettings()
    {
        using FakeAppSettingsService settingsService = new(
            new OutbreakTrackerSettings
            {
                RunReports = new RunReportSettings
                {
                    GenerateRunReports = true,
                    OutputDirectory = "custom-reports",
                    WriteMarkdown = true,
                    WriteCsv = false,
                    WriteHtml = true,
                },
                MemoryWatcher = new MemoryWatcherSettings
                {
                    Backend = MemoryBackendMode.MemoryWatcher,
                    PreferredBackend = WatchBackendKind.HardwareWatchpoint,
                    PreferredPrecision = WatchPrecision.HardwareAddressExact,
                    AllowFallback = false,
                    NativeLibraryPath = "C:\\mw\\MemoryWatcher.RemoteAot.dll",
                    AllowIntrusiveBackends = true,
                    EventBufferCapacity = 2048,
                    HashBlockSizeBytes = 128,
                    UseHashIndex = false,
                },
            }
        );

        Type viewModelType = typeof(OutbreakTracker2.Application.App).Assembly.GetType(
            "OutbreakTracker2.Application.Views.Settings.AppSettingsDialogViewModel",
            throwOnError: true
        )!;

        object viewModel = CreateViewModel(viewModelType, settingsService);

        await Assert
            .That((MemoryBackendMode)viewModelType.GetProperty("MemoryWatcherBackend")!.GetValue(viewModel)!)
            .IsEqualTo(MemoryBackendMode.MemoryWatcher);
        await Assert
            .That((string)viewModelType.GetProperty("ReportOutputDirectory")!.GetValue(viewModel)!)
            .IsEqualTo("custom-reports");

        viewModelType.GetProperty("ReportOutputDirectory")!.SetValue(viewModel, "moved-reports");
        viewModelType.GetProperty("MemoryWatcherPreferredBackend")!.SetValue(viewModel, WatchBackendKind.DirtyRange);
        viewModelType.GetProperty("MemoryWatcherEventBufferCapacity")!.SetValue(viewModel, 4096);
        viewModelType.GetProperty("MemoryWatcherHashBlockSizeBytes")!.SetValue(viewModel, 160);
        viewModelType.GetProperty("MemoryWatcherUseHashIndex")!.SetValue(viewModel, true);

        MethodInfo buildSettingsMethod = viewModelType.GetMethod(
            "BuildSettings",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;
        OutbreakTrackerSettings builtSettings = (OutbreakTrackerSettings)buildSettingsMethod.Invoke(viewModel, null)!;

        await Assert.That(builtSettings.RunReports.OutputDirectory).IsEqualTo("moved-reports");
        await Assert.That(builtSettings.MemoryWatcher.Backend).IsEqualTo(MemoryBackendMode.MemoryWatcher);
        await Assert.That(builtSettings.MemoryWatcher.PreferredBackend).IsEqualTo(WatchBackendKind.DirtyRange);
        await Assert
            .That(builtSettings.MemoryWatcher.PreferredPrecision)
            .IsEqualTo(WatchPrecision.DirtyRangeThenBitDiff);
        await Assert.That(builtSettings.MemoryWatcher.AllowFallback).IsFalse();
        await Assert
            .That(builtSettings.MemoryWatcher.NativeLibraryPath)
            .IsEqualTo("C:\\mw\\MemoryWatcher.RemoteAot.dll");
        await Assert.That(builtSettings.MemoryWatcher.AllowIntrusiveBackends).IsTrue();
        await Assert.That(builtSettings.MemoryWatcher.EventBufferCapacity).IsEqualTo(4096);
        await Assert.That(builtSettings.MemoryWatcher.HashBlockSizeBytes).IsEqualTo(160);
        await Assert.That(builtSettings.MemoryWatcher.UseHashIndex).IsTrue();
    }

    [Test]
    public async Task Constructor_LoadsCapabilityCards_AndSelectionSummary()
    {
        using FakeAppSettingsService settingsService = new(
            new OutbreakTrackerSettings
            {
                MemoryWatcher = new MemoryWatcherSettings
                {
                    Backend = MemoryBackendMode.MemoryWatcher,
                    PreferredBackend = WatchBackendKind.HardwareWatchpoint,
                    PreferredPrecision = WatchPrecision.HardwareAddressExact,
                    AllowFallback = false,
                },
            }
        );

        Type viewModelType = typeof(OutbreakTracker2.Application.App).Assembly.GetType(
            "OutbreakTracker2.Application.Views.Settings.AppSettingsDialogViewModel",
            throwOnError: true
        )!;

        object viewModel = CreateViewModel(viewModelType, settingsService);

        string hostSummary = (string)viewModelType.GetProperty("MemoryCapabilityHostSummary")!.GetValue(viewModel)!;
        string realitySummary = (string)
            viewModelType.GetProperty("MemoryCapabilityRealitySummary")!.GetValue(viewModel)!;
        string policySummary = (string)viewModelType.GetProperty("MemoryWatcherPolicySummary")!.GetValue(viewModel)!;
        string recommendedSummary = (string)
            viewModelType.GetProperty("MemoryRecommendedConfigurationSummary")!.GetValue(viewModel)!;
        string cadenceSummary = (string)viewModelType.GetProperty("MemoryCadenceSummary")!.GetValue(viewModel)!;
        System.Collections.IEnumerable backendCards = (System.Collections.IEnumerable)
            viewModelType.GetProperty("BackendCapabilityCards")!.GetValue(viewModel)!;
        System.Collections.IEnumerable strategyCards = (System.Collections.IEnumerable)
            viewModelType.GetProperty("MemoryWatcherCapabilityCards")!.GetValue(viewModel)!;

        await Assert.That(hostSummary.Contains("TestOS", StringComparison.Ordinal)).IsTrue();
        await Assert.That(realitySummary.Contains("Ready now:", StringComparison.Ordinal)).IsTrue();
        await Assert
            .That(realitySummary.Contains("Missing from current packaged runtime:", StringComparison.Ordinal))
            .IsTrue();
        await Assert.That(realitySummary.Contains("Needs live probe retry:", StringComparison.Ordinal)).IsTrue();
        await Assert.That(policySummary.Contains("Hardware Watchpoint", StringComparison.Ordinal)).IsTrue();
        await Assert
            .That(recommendedSummary.Contains("Best pick right now: MemoryWatcher", StringComparison.Ordinal))
            .IsTrue();
        await Assert
            .That(recommendedSummary.Contains("Resolved strategy today: Snapshot", StringComparison.Ordinal))
            .IsTrue();
        await Assert
            .That(
                cadenceSummary.Contains(
                    "event wakeups over grouped snapshot decoding",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            .IsTrue();
        await Assert.That(backendCards.Cast<object>().Any()).IsTrue();
        await Assert.That(strategyCards.Cast<object>().Any()).IsTrue();
    }

    [Test]
    public async Task UseRecommendedMemoryConfigurationCommand_ResetsToSafeRecommendedDefault()
    {
        using FakeAppSettingsService settingsService = new(
            new OutbreakTrackerSettings
            {
                MemoryWatcher = new MemoryWatcherSettings
                {
                    Backend = MemoryBackendMode.Legacy,
                    PreferredBackend = WatchBackendKind.HardwareWatchpoint,
                    PreferredPrecision = WatchPrecision.HardwareAddressExact,
                    AllowFallback = false,
                },
            }
        );

        Type viewModelType = typeof(OutbreakTracker2.Application.App).Assembly.GetType(
            "OutbreakTracker2.Application.Views.Settings.AppSettingsDialogViewModel",
            throwOnError: true
        )!;

        object viewModel = CreateViewModel(viewModelType, settingsService);

        ICommand command = (ICommand)
            viewModelType.GetProperty("UseRecommendedMemoryConfigurationCommand")!.GetValue(viewModel)!;
        command.Execute(null);

        await Assert
            .That((MemoryBackendMode)viewModelType.GetProperty("MemoryWatcherBackend")!.GetValue(viewModel)!)
            .IsEqualTo(MemoryBackendMode.MemoryWatcher);
        await Assert
            .That((WatchBackendKind)viewModelType.GetProperty("MemoryWatcherPreferredBackend")!.GetValue(viewModel)!)
            .IsEqualTo(WatchBackendKind.Auto);
        await Assert
            .That((WatchPrecision)viewModelType.GetProperty("MemoryWatcherPreferredPrecision")!.GetValue(viewModel)!)
            .IsEqualTo(WatchPrecision.SnapshotBitExact);
        await Assert.That((bool)viewModelType.GetProperty("MemoryWatcherAllowFallback")!.GetValue(viewModel)!).IsTrue();
        await Assert
            .That((bool)viewModelType.GetProperty("MemoryWatcherAllowIntrusiveBackends")!.GetValue(viewModel)!)
            .IsFalse();
    }

    [Test]
    public async Task MemoryWatcherPreferredBackendChange_EnablesIntrusiveOptIn_ForHardwareWatchpoint()
    {
        using FakeAppSettingsService settingsService = new(
            new OutbreakTrackerSettings
            {
                MemoryWatcher = new MemoryWatcherSettings
                {
                    Backend = MemoryBackendMode.MemoryWatcher,
                    PreferredBackend = WatchBackendKind.Auto,
                    PreferredPrecision = WatchPrecision.SnapshotBitExact,
                    AllowFallback = true,
                    AllowIntrusiveBackends = false,
                },
            }
        );

        Type viewModelType = typeof(OutbreakTracker2.Application.App).Assembly.GetType(
            "OutbreakTracker2.Application.Views.Settings.AppSettingsDialogViewModel",
            throwOnError: true
        )!;

        object viewModel = CreateViewModel(viewModelType, settingsService);
        viewModelType
            .GetProperty("MemoryWatcherPreferredBackend")!
            .SetValue(viewModel, WatchBackendKind.HardwareWatchpoint);

        await Assert
            .That((WatchBackendKind)viewModelType.GetProperty("MemoryWatcherPreferredBackend")!.GetValue(viewModel)!)
            .IsEqualTo(WatchBackendKind.HardwareWatchpoint);
        await Assert
            .That((WatchPrecision)viewModelType.GetProperty("MemoryWatcherPreferredPrecision")!.GetValue(viewModel)!)
            .IsEqualTo(WatchPrecision.HardwareAddressExact);
        await Assert
            .That((bool)viewModelType.GetProperty("MemoryWatcherAllowIntrusiveBackends")!.GetValue(viewModel)!)
            .IsTrue();
    }

    [Test]
    public async Task MemoryWatcherPreferredBackendChange_BlocksSave_ForHelperOnlyBackend()
    {
        using FakeAppSettingsService settingsService = new(
            new OutbreakTrackerSettings
            {
                MemoryWatcher = new MemoryWatcherSettings
                {
                    Backend = MemoryBackendMode.MemoryWatcher,
                    PreferredBackend = WatchBackendKind.Auto,
                    PreferredPrecision = WatchPrecision.SnapshotBitExact,
                    AllowFallback = true,
                    AllowIntrusiveBackends = false,
                },
            }
        );

        Type viewModelType = typeof(OutbreakTracker2.Application.App).Assembly.GetType(
            "OutbreakTracker2.Application.Views.Settings.AppSettingsDialogViewModel",
            throwOnError: true
        )!;

        object viewModel = CreateViewModel(viewModelType, settingsService);
        viewModelType.GetProperty("MemoryWatcherPreferredBackend")!.SetValue(viewModel, WatchBackendKind.DirtyRange);

        string? validationMessage = (string?)viewModelType.GetProperty("ValidationMessage")!.GetValue(viewModel);
        ICommand saveCommand = (ICommand)viewModelType.GetProperty("SaveCommand")!.GetValue(viewModel)!;

        await Assert.That(validationMessage).IsNotNull();
        await Assert
            .That(validationMessage!.Contains("cooperative helper", StringComparison.OrdinalIgnoreCase))
            .IsTrue();
        await Assert.That(saveCommand.CanExecute(null)).IsFalse();
    }

    [Test]
    public async Task UseMemoryWatcherCapabilityCommand_EnablesIntrusiveOptIn_ForHardwareWatchpoint()
    {
        using FakeAppSettingsService settingsService = new(
            new OutbreakTrackerSettings
            {
                MemoryWatcher = new MemoryWatcherSettings
                {
                    Backend = MemoryBackendMode.MemoryWatcher,
                    PreferredBackend = WatchBackendKind.Auto,
                    PreferredPrecision = WatchPrecision.SnapshotBitExact,
                    AllowFallback = true,
                    AllowIntrusiveBackends = false,
                },
            }
        );

        Type viewModelType = typeof(OutbreakTracker2.Application.App).Assembly.GetType(
            "OutbreakTracker2.Application.Views.Settings.AppSettingsDialogViewModel",
            throwOnError: true
        )!;

        object viewModel = CreateViewModel(viewModelType, settingsService);

        System.Collections.IEnumerable strategyCards = (System.Collections.IEnumerable)
            viewModelType.GetProperty("MemoryWatcherCapabilityCards")!.GetValue(viewModel)!;
        object hardwareCard = strategyCards
            .Cast<object>()
            .First(card =>
                string.Equals(
                    (string)card.GetType().GetProperty("Title")!.GetValue(card)!,
                    "Hardware Watchpoint",
                    StringComparison.Ordinal
                )
            );

        ICommand command = (ICommand)
            viewModelType.GetProperty("UseMemoryWatcherCapabilityCommand")!.GetValue(viewModel)!;
        command.Execute(hardwareCard);

        await Assert
            .That((WatchBackendKind)viewModelType.GetProperty("MemoryWatcherPreferredBackend")!.GetValue(viewModel)!)
            .IsEqualTo(WatchBackendKind.HardwareWatchpoint);
        await Assert
            .That((bool)viewModelType.GetProperty("MemoryWatcherAllowIntrusiveBackends")!.GetValue(viewModel)!)
            .IsTrue();
        await Assert
            .That((bool)viewModelType.GetProperty("MemoryWatcherAllowFallback")!.GetValue(viewModel)!)
            .IsFalse();
    }

    [Test]
    public async Task Constructor_AnnotatesPreferredBackendOptions_WithLiveCapabilityState()
    {
        using FakeAppSettingsService settingsService = new(new OutbreakTrackerSettings());

        Type viewModelType = typeof(OutbreakTracker2.Application.App).Assembly.GetType(
            "OutbreakTracker2.Application.Views.Settings.AppSettingsDialogViewModel",
            throwOnError: true
        )!;

        object viewModel = CreateViewModel(viewModelType, settingsService);

        System.Collections.IEnumerable backendOptions = (System.Collections.IEnumerable)
            viewModelType.GetProperty("PreferredMemoryWatcherBackendOptions")!.GetValue(viewModel)!;
        object autoOption = backendOptions
            .Cast<object>()
            .First(option =>
                ((WatchBackendKind)option.GetType().GetProperty("Value")!.GetValue(option)!) == WatchBackendKind.Auto
            );
        object hardwareOption = backendOptions
            .Cast<object>()
            .First(option =>
                ((WatchBackendKind)option.GetType().GetProperty("Value")!.GetValue(option)!)
                == WatchBackendKind.HardwareWatchpoint
            );

        string autoStateSummary = (string)autoOption.GetType().GetProperty("StateSummary")!.GetValue(autoOption)!;
        string hardwareStateSummary = (string)
            hardwareOption.GetType().GetProperty("StateSummary")!.GetValue(hardwareOption)!;
        string hardwareStatusLabel = (string)
            hardwareOption.GetType().GetProperty("StatusLabel")!.GetValue(hardwareOption)!;
        string selectedStateSummary = (string)
            viewModelType.GetProperty("SelectedMemoryWatcherPreferredBackendStateSummary")!.GetValue(viewModel)!;

        await Assert.That(autoStateSummary.Contains("Current state:", StringComparison.Ordinal)).IsTrue();
        await Assert.That(hardwareStateSummary.Contains("Current state:", StringComparison.Ordinal)).IsTrue();
        await Assert.That(hardwareStatusLabel).IsEqualTo("Ready now");
        await Assert.That(selectedStateSummary.Contains("Current state:", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Constructor_FiltersPreferredBackendOptions_ToOt2SelectableChoices()
    {
        using FakeAppSettingsService settingsService = new(new OutbreakTrackerSettings());

        Type viewModelType = typeof(OutbreakTracker2.Application.App).Assembly.GetType(
            "OutbreakTracker2.Application.Views.Settings.AppSettingsDialogViewModel",
            throwOnError: true
        )!;

        object viewModel = CreateViewModel(viewModelType, settingsService);

        System.Collections.IEnumerable backendOptions = (System.Collections.IEnumerable)
            viewModelType.GetProperty("PreferredMemoryWatcherBackendOptions")!.GetValue(viewModel)!;
        WatchBackendKind[] values = backendOptions
            .Cast<object>()
            .Select(option => (WatchBackendKind)option.GetType().GetProperty("Value")!.GetValue(option)!)
            .ToArray();

        await Assert.That(values).Contains(WatchBackendKind.Auto);
        await Assert.That(values).Contains(WatchBackendKind.Snapshot);
        await Assert.That(values).Contains(WatchBackendKind.HashIndexedSnapshot);
        await Assert.That(values).Contains(WatchBackendKind.SegmentedSnapshot);
        await Assert.That(values).Contains(WatchBackendKind.PageFault);
        await Assert.That(values).Contains(WatchBackendKind.HardwareWatchpoint);
        await Assert.That(values.Contains(WatchBackendKind.DirtyRange)).IsFalse();
        await Assert.That(values.Contains(WatchBackendKind.NativeAgent)).IsFalse();
        await Assert.That(values.Contains(WatchBackendKind.DirtyPage)).IsFalse();
        await Assert.That(values.Contains(WatchBackendKind.SoftDirty)).IsFalse();
    }

    [Test]
    public async Task Constructor_AnnotatesPreferredPrecisionOptions_WithResolvedCapabilityState()
    {
        using FakeAppSettingsService settingsService = new(new OutbreakTrackerSettings());

        Type viewModelType = typeof(OutbreakTracker2.Application.App).Assembly.GetType(
            "OutbreakTracker2.Application.Views.Settings.AppSettingsDialogViewModel",
            throwOnError: true
        )!;

        object viewModel = CreateViewModel(viewModelType, settingsService);

        System.Collections.IEnumerable precisionOptions = (System.Collections.IEnumerable)
            viewModelType.GetProperty("PreferredMemoryWatcherPrecisionOptions")!.GetValue(viewModel)!;
        object snapshotOption = precisionOptions
            .Cast<object>()
            .First(option =>
                ((WatchPrecision)option.GetType().GetProperty("Value")!.GetValue(option)!)
                == WatchPrecision.SnapshotBitExact
            );
        object pageFaultOption = precisionOptions
            .Cast<object>()
            .First(option =>
                ((WatchPrecision)option.GetType().GetProperty("Value")!.GetValue(option)!)
                == WatchPrecision.PageFaultThenBitDiff
            );
        object hardwareOption = precisionOptions
            .Cast<object>()
            .First(option =>
                ((WatchPrecision)option.GetType().GetProperty("Value")!.GetValue(option)!)
                == WatchPrecision.HardwareAddressExact
            );

        string snapshotStatusLabel = (string)
            snapshotOption.GetType().GetProperty("StatusLabel")!.GetValue(snapshotOption)!;
        string pageFaultStatusLabel = (string)
            pageFaultOption.GetType().GetProperty("StatusLabel")!.GetValue(pageFaultOption)!;
        string pageFaultStateSummary = (string)
            pageFaultOption.GetType().GetProperty("StateSummary")!.GetValue(pageFaultOption)!;
        string hardwareStatusLabel = (string)
            hardwareOption.GetType().GetProperty("StatusLabel")!.GetValue(hardwareOption)!;
        WatchPrecision[] values = precisionOptions
            .Cast<object>()
            .Select(option => (WatchPrecision)option.GetType().GetProperty("Value")!.GetValue(option)!)
            .ToArray();

        await Assert.That(snapshotStatusLabel).IsEqualTo("Ready now");
        await Assert.That(pageFaultStatusLabel).IsEqualTo("Retry live probe");
        await Assert.That(pageFaultStateSummary.Contains("Page Fault", StringComparison.Ordinal)).IsTrue();
        await Assert.That(hardwareStatusLabel).IsEqualTo("Ready now");
        await Assert.That(values.Contains(WatchPrecision.DirtyRangeThenBitDiff)).IsFalse();
        await Assert.That(values.Contains(WatchPrecision.DirtyPageThenBitDiff)).IsFalse();
        await Assert.That(values.Contains(WatchPrecision.SoftDirtyThenBitDiff)).IsFalse();
    }

    [Test]
    public async Task Constructor_PreservesConfiguredUnsupportedBackendOption_InPicker()
    {
        using FakeAppSettingsService settingsService = new(
            new OutbreakTrackerSettings
            {
                MemoryWatcher = new MemoryWatcherSettings
                {
                    Backend = MemoryBackendMode.MemoryWatcher,
                    PreferredBackend = WatchBackendKind.DirtyRange,
                    PreferredPrecision = WatchPrecision.DirtyRangeThenBitDiff,
                    AllowFallback = false,
                },
            }
        );

        Type viewModelType = typeof(OutbreakTracker2.Application.App).Assembly.GetType(
            "OutbreakTracker2.Application.Views.Settings.AppSettingsDialogViewModel",
            throwOnError: true
        )!;

        object viewModel = CreateViewModel(viewModelType, settingsService);

        System.Collections.IEnumerable backendOptions = (System.Collections.IEnumerable)
            viewModelType.GetProperty("PreferredMemoryWatcherBackendOptions")!.GetValue(viewModel)!;
        WatchBackendKind[] values = backendOptions
            .Cast<object>()
            .Select(option => (WatchBackendKind)option.GetType().GetProperty("Value")!.GetValue(option)!)
            .ToArray();
        string scopeSummary = (string)
            viewModelType.GetProperty("MemoryWatcherRequestPolicyScopeSummary")!.GetValue(viewModel)!;

        await Assert.That(values).Contains(WatchBackendKind.DirtyRange);
        await Assert.That(scopeSummary.Contains("Automatic", StringComparison.Ordinal)).IsTrue();
        await Assert
            .That(scopeSummary.Contains("safest compatible grouped-read default", StringComparison.OrdinalIgnoreCase))
            .IsTrue();
        await Assert.That(scopeSummary.Contains("Helper-only", StringComparison.OrdinalIgnoreCase)).IsTrue();
    }

    [Test]
    public async Task Constructor_DetachedScan_StillExposesDeferredPageFaultAndHardwareChoices()
    {
        using FakeAppSettingsService settingsService = new(
            new OutbreakTrackerSettings
            {
                MemoryWatcher = new MemoryWatcherSettings
                {
                    Backend = MemoryBackendMode.MemoryWatcher,
                    PreferredBackend = WatchBackendKind.Auto,
                    PreferredPrecision = WatchPrecision.SnapshotBitExact,
                    AllowFallback = true,
                },
            }
        );

        Type viewModelType = typeof(OutbreakTracker2.Application.App).Assembly.GetType(
            "OutbreakTracker2.Application.Views.Settings.AppSettingsDialogViewModel",
            throwOnError: true
        )!;

        object viewModel = CreateViewModel(
            viewModelType,
            settingsService,
            new DetachedFakeMemoryBackendCapabilityService()
        );

        System.Collections.IEnumerable backendOptions = (System.Collections.IEnumerable)
            viewModelType.GetProperty("PreferredMemoryWatcherBackendOptions")!.GetValue(viewModel)!;
        WatchBackendKind[] backendValues = backendOptions
            .Cast<object>()
            .Select(option => (WatchBackendKind)option.GetType().GetProperty("Value")!.GetValue(option)!)
            .ToArray();

        System.Collections.IEnumerable precisionOptions = (System.Collections.IEnumerable)
            viewModelType.GetProperty("PreferredMemoryWatcherPrecisionOptions")!.GetValue(viewModel)!;
        WatchPrecision[] precisionValues = precisionOptions
            .Cast<object>()
            .Select(option => (WatchPrecision)option.GetType().GetProperty("Value")!.GetValue(option)!)
            .ToArray();

        System.Collections.IEnumerable strategyCards = (System.Collections.IEnumerable)
            viewModelType.GetProperty("MemoryWatcherCapabilityCards")!.GetValue(viewModel)!;
        object pageFaultCard = strategyCards
            .Cast<object>()
            .First(card =>
                string.Equals(
                    (string)card.GetType().GetProperty("Title")!.GetValue(card)!,
                    "Page Fault",
                    StringComparison.Ordinal
                )
            );

        string? validationMessage = (string?)viewModelType.GetProperty("ValidationMessage")!.GetValue(viewModel);
        ICommand saveCommand = (ICommand)viewModelType.GetProperty("SaveCommand")!.GetValue(viewModel)!;
        bool pageFaultCanApply = (bool)pageFaultCard.GetType().GetProperty("CanApply")!.GetValue(pageFaultCard)!;
        string pageFaultButtonText = (string)
            pageFaultCard.GetType().GetProperty("ButtonText")!.GetValue(pageFaultCard)!;
        string cadenceSummary = (string)viewModelType.GetProperty("MemoryCadenceSummary")!.GetValue(viewModel)!;
        ICommand useCapabilityCommand = (ICommand)
            viewModelType.GetProperty("UseMemoryWatcherCapabilityCommand")!.GetValue(viewModel)!;
        useCapabilityCommand.Execute(pageFaultCard);
        bool allowFallback = (bool)viewModelType.GetProperty("MemoryWatcherAllowFallback")!.GetValue(viewModel)!;

        await Assert.That(backendValues).Contains(WatchBackendKind.PageFault);
        await Assert.That(backendValues).Contains(WatchBackendKind.HardwareWatchpoint);
        await Assert.That(precisionValues).Contains(WatchPrecision.PageFaultThenBitDiff);
        await Assert.That(pageFaultCanApply).IsTrue();
        await Assert.That(pageFaultButtonText).IsEqualTo("Use And Validate Later");
        await Assert.That(validationMessage).IsNull();
        await Assert.That(saveCommand.CanExecute(null)).IsTrue();
        await Assert.That(allowFallback).IsTrue();
        await Assert
            .That(
                cadenceSummary.Contains(
                    "deferred MemoryWatcher request awaiting live validation",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            .IsTrue();
    }

    [Test]
    public async Task Constructor_DetachedScan_RecommendsMemoryWatcherDeferredBaseline()
    {
        using FakeAppSettingsService settingsService = new(new OutbreakTrackerSettings());

        Type viewModelType = typeof(OutbreakTracker2.Application.App).Assembly.GetType(
            "OutbreakTracker2.Application.Views.Settings.AppSettingsDialogViewModel",
            throwOnError: true
        )!;

        object viewModel = CreateViewModel(
            viewModelType,
            settingsService,
            new DetachedFakeMemoryBackendCapabilityService()
        );

        string recommendedSummary = (string)
            viewModelType.GetProperty("MemoryRecommendedConfigurationSummary")!.GetValue(viewModel)!;

        await Assert
            .That(recommendedSummary.Contains("Best pick right now: MemoryWatcher", StringComparison.Ordinal))
            .IsTrue();
        await Assert
            .That(recommendedSummary.Contains("Safe deferred baseline: Snapshot", StringComparison.Ordinal))
            .IsTrue();
        await Assert
            .That(recommendedSummary.Contains("validate after attach", StringComparison.OrdinalIgnoreCase))
            .IsTrue();
    }

    [Test]
    public async Task UseRecommendedMemoryConfigurationCommand_DetachedScan_KeepsMemoryWatcherBaseline()
    {
        using FakeAppSettingsService settingsService = new(
            new OutbreakTrackerSettings
            {
                MemoryWatcher = new MemoryWatcherSettings
                {
                    Backend = MemoryBackendMode.Legacy,
                    PreferredBackend = WatchBackendKind.HardwareWatchpoint,
                    PreferredPrecision = WatchPrecision.HardwareAddressExact,
                    AllowFallback = false,
                    AllowIntrusiveBackends = true,
                },
            }
        );

        Type viewModelType = typeof(OutbreakTracker2.Application.App).Assembly.GetType(
            "OutbreakTracker2.Application.Views.Settings.AppSettingsDialogViewModel",
            throwOnError: true
        )!;

        object viewModel = CreateViewModel(
            viewModelType,
            settingsService,
            new DetachedFakeMemoryBackendCapabilityService()
        );

        ICommand command = (ICommand)
            viewModelType.GetProperty("UseRecommendedMemoryConfigurationCommand")!.GetValue(viewModel)!;
        command.Execute(null);

        await Assert
            .That((MemoryBackendMode)viewModelType.GetProperty("MemoryWatcherBackend")!.GetValue(viewModel)!)
            .IsEqualTo(MemoryBackendMode.MemoryWatcher);
        await Assert
            .That((WatchBackendKind)viewModelType.GetProperty("MemoryWatcherPreferredBackend")!.GetValue(viewModel)!)
            .IsEqualTo(WatchBackendKind.Auto);
        await Assert
            .That((WatchPrecision)viewModelType.GetProperty("MemoryWatcherPreferredPrecision")!.GetValue(viewModel)!)
            .IsEqualTo(WatchPrecision.SnapshotBitExact);
        await Assert.That((bool)viewModelType.GetProperty("MemoryWatcherAllowFallback")!.GetValue(viewModel)!).IsTrue();
        await Assert
            .That((bool)viewModelType.GetProperty("MemoryWatcherAllowIntrusiveBackends")!.GetValue(viewModel)!)
            .IsFalse();
    }

    [Test]
    public async Task Constructor_ClassifiesPackagedRuntimeMissingPageTrackerAsUnavailableHere()
    {
        using FakeAppSettingsService settingsService = new(new OutbreakTrackerSettings());

        Type viewModelType = typeof(OutbreakTracker2.Application.App).Assembly.GetType(
            "OutbreakTracker2.Application.Views.Settings.AppSettingsDialogViewModel",
            throwOnError: true
        )!;

        object viewModel = CreateViewModel(viewModelType, settingsService);

        System.Collections.IEnumerable strategyCards = (System.Collections.IEnumerable)
            viewModelType.GetProperty("MemoryWatcherCapabilityCards")!.GetValue(viewModel)!;
        object dirtyPageCard = strategyCards
            .Cast<object>()
            .First(card =>
                string.Equals(
                    (string)card.GetType().GetProperty("Title")!.GetValue(card)!,
                    "Dirty Page",
                    StringComparison.Ordinal
                )
            );

        Type cardType = dirtyPageCard.GetType();
        string supportLabel = (string)cardType.GetProperty("SupportLabel")!.GetValue(dirtyPageCard)!;
        string hostSupportLabel = (string)cardType.GetProperty("HostSupportLabel")!.GetValue(dirtyPageCard)!;
        string constraintLabel = (string)cardType.GetProperty("ConstraintLabel")!.GetValue(dirtyPageCard)!;
        string hostSummary = (string)cardType.GetProperty("HostSummary")!.GetValue(dirtyPageCard)!;
        bool canApply = (bool)cardType.GetProperty("CanApply")!.GetValue(dirtyPageCard)!;

        await Assert.That(supportLabel).IsEqualTo("OT2: Unavailable here");
        await Assert.That(hostSupportLabel).IsEqualTo("Host: Runtime missing");
        await Assert.That(constraintLabel).IsEqualTo("Unavailable in packaged runtime");
        await Assert.That(hostSummary.Contains("does not surface", StringComparison.OrdinalIgnoreCase)).IsTrue();
        await Assert.That(canApply).IsFalse();
    }

    [Test]
    public async Task Constructor_DoesNotTreatPageFaultAsPackagedRuntimeMissing()
    {
        using FakeAppSettingsService settingsService = new(new OutbreakTrackerSettings());

        Type viewModelType = typeof(OutbreakTracker2.Application.App).Assembly.GetType(
            "OutbreakTracker2.Application.Views.Settings.AppSettingsDialogViewModel",
            throwOnError: true
        )!;

        object viewModel = CreateViewModel(viewModelType, settingsService);

        System.Collections.IEnumerable strategyCards = (System.Collections.IEnumerable)
            viewModelType.GetProperty("MemoryWatcherCapabilityCards")!.GetValue(viewModel)!;
        object pageFaultCard = strategyCards
            .Cast<object>()
            .First(card =>
                string.Equals(
                    (string)card.GetType().GetProperty("Title")!.GetValue(card)!,
                    "Page Fault",
                    StringComparison.Ordinal
                )
            );

        Type cardType = pageFaultCard.GetType();
        string supportLabel = (string)cardType.GetProperty("SupportLabel")!.GetValue(pageFaultCard)!;
        string hostSupportLabel = (string)cardType.GetProperty("HostSupportLabel")!.GetValue(pageFaultCard)!;
        string constraintLabel = (string)cardType.GetProperty("ConstraintLabel")!.GetValue(pageFaultCard)!;
        string hostSummary = (string)cardType.GetProperty("HostSummary")!.GetValue(pageFaultCard)!;

        await Assert.That(supportLabel).IsEqualTo("OT2: Retry live probe");
        await Assert.That(hostSupportLabel).IsEqualTo("Host: Host supports it");
        await Assert.That(constraintLabel).IsEqualTo("Probe did not arm backend");
        await Assert.That(hostSummary.Contains("does not surface", StringComparison.OrdinalIgnoreCase)).IsFalse();
    }

    [Test]
    public async Task Constructor_ExposesSnapshotFamilyCards()
    {
        using FakeAppSettingsService settingsService = new(new OutbreakTrackerSettings());

        Type viewModelType = typeof(OutbreakTracker2.Application.App).Assembly.GetType(
            "OutbreakTracker2.Application.Views.Settings.AppSettingsDialogViewModel",
            throwOnError: true
        )!;

        object viewModel = CreateViewModel(viewModelType, settingsService);

        System.Collections.IEnumerable strategyCards = (System.Collections.IEnumerable)
            viewModelType.GetProperty("MemoryWatcherCapabilityCards")!.GetValue(viewModel)!;
        string[] titles = strategyCards
            .Cast<object>()
            .Select(card => (string)card.GetType().GetProperty("Title")!.GetValue(card)!)
            .ToArray();

        await Assert.That(titles.Contains("Snapshot")).IsTrue();
        await Assert.That(titles.Contains("Hash Indexed Snapshot")).IsTrue();
        await Assert.That(titles.Contains("Segmented Snapshot")).IsTrue();
    }

    private static object CreateViewModel(
        Type viewModelType,
        FakeAppSettingsService settingsService,
        IMemoryBackendCapabilityService? capabilityService = null
    ) =>
        Activator.CreateInstance(
            viewModelType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args:
            [
                settingsService,
                capabilityService ?? new FakeMemoryBackendCapabilityService(),
                new FakeProcessLauncher(),
                CreateProxy<ISukiToastManager>(),
                CreateProxy<ISukiDialog>(),
            ],
            culture: null
        )!;

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

    private sealed class FakeMemoryBackendCapabilityService : IMemoryBackendCapabilityService
    {
        public MemoryBackendCapabilityReport Inspect(
            IGameClient? gameClient,
            OutbreakTrackerSettings? settingsOverride = null
        )
        {
            OutbreakTrackerSettings settings = settingsOverride ?? new OutbreakTrackerSettings();
            return new MemoryBackendCapabilityReport
            {
                Host = new MemoryWatchHostEnvironment
                {
                    OperatingSystem = "TestOS",
                    RuntimeDescription = ".NET Test",
                    ProcessArchitecture = System.Runtime.InteropServices.Architecture.X64,
                    Is64BitProcess = true,
                    UserName = "tester",
                    IsElevatedUser = false,
                    SupportsPackagedRemoteAot = true,
                    SupportsSoftDirtyTracking = false,
                    SupportsDebuggerMediatedHardwareWatchpoints = true,
                },
                MemoryWatcher = new MemoryWatchCapabilityNegotiationResult
                {
                    Host = new MemoryWatchHostEnvironment
                    {
                        OperatingSystem = "TestOS",
                        RuntimeDescription = ".NET Test",
                        ProcessArchitecture = System.Runtime.InteropServices.Architecture.X64,
                        Is64BitProcess = true,
                        UserName = "tester",
                        IsElevatedUser = false,
                        SupportsPackagedRemoteAot = true,
                        SupportsSoftDirtyTracking = false,
                        SupportsDebuggerMediatedHardwareWatchpoints = true,
                    },
                    Target = new MemoryWatchTargetEnvironment
                    {
                        ProcessId = 1337,
                        ProcessName = "pcsx2",
                        ProcessFound = true,
                        SessionOpened = true,
                    },
                    Capabilities =
                    [
                        new MemoryWatchNegotiatedCapability
                        {
                            Backend = WatchBackendKind.Snapshot,
                            BackendName = "snapshot-bit-diff",
                            Invasiveness = MemoryObservationInvasiveness.OutOfProcess,
                            PrecisionClass = MemoryObservationPrecisionClass.SampledFinalValue,
                            LatencyClass = MemoryObservationLatencyClass.UnknownOrCallerDriven,
                            EnvironmentSupport = MemoryCapabilitySupportLevel.Supported,
                            EnvironmentConstraintKind = MemoryCapabilityConstraintKind.None,
                            CurrentCapability = new WatchCapability
                            {
                                BackendName = "snapshot-bit-diff",
                                Precision = WatchPrecision.SnapshotBitExact,
                                Safety = WatchSafety.SafeManaged,
                                IsAvailable = true,
                                EventDriven = false,
                                Intrusive = false,
                                RequiresAgent = false,
                                RequiresUnsafe = false,
                                BitExact = true,
                                EdgeExact = false,
                                CanMissAbaBetweenSamples = true,
                                TriggerGranularityBits = 8,
                                AlignmentRequirementBytes = 1,
                                RequiresThreadIdList = false,
                            },
                            CurrentRequestAvailable = true,
                            CurrentRequestConstraintKind = MemoryCapabilityConstraintKind.None,
                        },
                        new MemoryWatchNegotiatedCapability
                        {
                            Backend = WatchBackendKind.HashIndexedSnapshot,
                            BackendName = "hash-indexed-snapshot",
                            Invasiveness = MemoryObservationInvasiveness.OutOfProcess,
                            PrecisionClass = MemoryObservationPrecisionClass.SampledFinalValue,
                            LatencyClass = MemoryObservationLatencyClass.UnknownOrCallerDriven,
                            EnvironmentSupport = MemoryCapabilitySupportLevel.Supported,
                            EnvironmentConstraintKind = MemoryCapabilityConstraintKind.None,
                            CurrentCapability = new WatchCapability
                            {
                                BackendName = "hash-indexed-snapshot",
                                Precision = WatchPrecision.SnapshotBitExact,
                                Safety = WatchSafety.SafeManaged,
                                IsAvailable = true,
                                EventDriven = false,
                                Intrusive = false,
                                RequiresAgent = false,
                                RequiresUnsafe = false,
                                BitExact = true,
                                EdgeExact = false,
                                CanMissAbaBetweenSamples = true,
                                TriggerGranularityBits = 8,
                                AlignmentRequirementBytes = 1,
                                RequiresThreadIdList = false,
                            },
                            CurrentRequestAvailable = true,
                            CurrentRequestConstraintKind = MemoryCapabilityConstraintKind.None,
                        },
                        new MemoryWatchNegotiatedCapability
                        {
                            Backend = WatchBackendKind.SegmentedSnapshot,
                            BackendName = "segmented-snapshot",
                            Invasiveness = MemoryObservationInvasiveness.OutOfProcess,
                            PrecisionClass = MemoryObservationPrecisionClass.SampledFinalValue,
                            LatencyClass = MemoryObservationLatencyClass.UnknownOrCallerDriven,
                            EnvironmentSupport = MemoryCapabilitySupportLevel.Supported,
                            EnvironmentConstraintKind = MemoryCapabilityConstraintKind.None,
                            CurrentCapability = new WatchCapability
                            {
                                BackendName = "segmented-snapshot",
                                Precision = WatchPrecision.SnapshotBitExact,
                                Safety = WatchSafety.SafeManaged,
                                IsAvailable = true,
                                EventDriven = false,
                                Intrusive = false,
                                RequiresAgent = false,
                                RequiresUnsafe = false,
                                BitExact = true,
                                EdgeExact = false,
                                CanMissAbaBetweenSamples = true,
                                TriggerGranularityBits = 8,
                                AlignmentRequirementBytes = 1,
                                RequiresThreadIdList = false,
                            },
                            CurrentRequestAvailable = true,
                            CurrentRequestConstraintKind = MemoryCapabilityConstraintKind.None,
                        },
                        new MemoryWatchNegotiatedCapability
                        {
                            Backend = WatchBackendKind.DirtyRange,
                            BackendName = "dirty-range-then-diff",
                            Invasiveness = MemoryObservationInvasiveness.ExecutableHook,
                            PrecisionClass = MemoryObservationPrecisionClass.SignaledFinalValue,
                            LatencyClass = MemoryObservationLatencyClass.Under1Millisecond,
                            EnvironmentSupport = MemoryCapabilitySupportLevel.Conditional,
                            EnvironmentConstraintKind = MemoryCapabilityConstraintKind.RequiresCooperativeProducer,
                            EnvironmentSupportReason =
                                "Exact dirty-range observation requires a cooperative producer or in-process helper that can publish changed spans.",
                            CurrentCapability = null,
                            CurrentRequestAvailable = false,
                            CurrentRequestConstraintKind = MemoryCapabilityConstraintKind.RequiresCooperativeProducer,
                            CurrentRequestReason =
                                "Exact dirty-range observation requires a cooperative producer or in-process helper that can publish changed spans.",
                        },
                        new MemoryWatchNegotiatedCapability
                        {
                            Backend = WatchBackendKind.DirtyPage,
                            BackendName = "dirty-page-then-diff",
                            Invasiveness = MemoryObservationInvasiveness.OperatingSystemHook,
                            PrecisionClass = MemoryObservationPrecisionClass.SignaledFinalValue,
                            LatencyClass = MemoryObservationLatencyClass.Under1Millisecond,
                            EnvironmentSupport = MemoryCapabilitySupportLevel.Conditional,
                            EnvironmentConstraintKind = MemoryCapabilityConstraintKind.RequiresPageTracker,
                            EnvironmentSupportReason =
                                "Dirty-page observation requires page markers or another OS-mediated producer path beyond a plain external reader.",
                            CurrentCapability = null,
                            CurrentRequestAvailable = false,
                            CurrentRequestConstraintKind = MemoryCapabilityConstraintKind.BackendNotSurfacedForRegion,
                            CurrentRequestReason =
                                "The current engine did not surface this backend for the requested region.",
                        },
                        new MemoryWatchNegotiatedCapability
                        {
                            Backend = WatchBackendKind.PageFault,
                            BackendName = "page-fault-then-diff",
                            Invasiveness = MemoryObservationInvasiveness.OperatingSystemHook,
                            PrecisionClass = MemoryObservationPrecisionClass.SignaledFinalValue,
                            LatencyClass = MemoryObservationLatencyClass.Under1Millisecond,
                            EnvironmentSupport = MemoryCapabilitySupportLevel.Supported,
                            EnvironmentConstraintKind = MemoryCapabilityConstraintKind.None,
                            EnvironmentSupportReason =
                                "Platform support is present and the packaged runtime can arm debugger-mediated PAGE_GUARD watches for external targets.",
                            CurrentCapability = null,
                            CurrentRequestAvailable = false,
                            CurrentRequestConstraintKind = MemoryCapabilityConstraintKind.BackendNotSurfacedForRegion,
                            CurrentRequestReason =
                                "The current engine did not surface this backend for the requested region.",
                        },
                        new MemoryWatchNegotiatedCapability
                        {
                            Backend = WatchBackendKind.HardwareWatchpoint,
                            BackendName = "hardware-watchpoint",
                            Invasiveness = MemoryObservationInvasiveness.OperatingSystemHook,
                            PrecisionClass = MemoryObservationPrecisionClass.TransientEdgeExact,
                            LatencyClass = MemoryObservationLatencyClass.Under1Millisecond,
                            EnvironmentSupport = MemoryCapabilitySupportLevel.Supported,
                            EnvironmentConstraintKind = MemoryCapabilityConstraintKind.None,
                            CurrentCapability = new WatchCapability
                            {
                                BackendName = "hardware-watchpoint",
                                Precision = WatchPrecision.HardwareAddressExact,
                                Safety = WatchSafety.SafePublicNativeInternals,
                                IsAvailable = true,
                                EventDriven = true,
                                Intrusive = true,
                                RequiresAgent = false,
                                RequiresUnsafe = false,
                                BitExact = true,
                                EdgeExact = true,
                                CanMissAbaBetweenSamples = false,
                                TriggerGranularityBits = 8,
                                AlignmentRequirementBytes = 1,
                                RequiresThreadIdList = true,
                            },
                            CurrentRequestAvailable = true,
                            CurrentRequestConstraintKind = MemoryCapabilityConstraintKind.None,
                        },
                        new MemoryWatchNegotiatedCapability
                        {
                            Backend = WatchBackendKind.NativeAgent,
                            BackendName = "native-agent",
                            Invasiveness = MemoryObservationInvasiveness.ExecutableHook,
                            PrecisionClass = MemoryObservationPrecisionClass.SignaledFinalValue,
                            LatencyClass = MemoryObservationLatencyClass.Under1Millisecond,
                            EnvironmentSupport = MemoryCapabilitySupportLevel.Conditional,
                            EnvironmentConstraintKind = MemoryCapabilityConstraintKind.RequiresAgent,
                            EnvironmentSupportReason =
                                "Native-agent observation requires loading a cooperative helper inside the target process.",
                            CurrentCapability = null,
                            CurrentRequestAvailable = false,
                            CurrentRequestConstraintKind = MemoryCapabilityConstraintKind.RequiresAgent,
                            CurrentRequestReason =
                                "Native-agent observation requires loading a cooperative helper inside the target process.",
                        },
                    ],
                },
                Backends =
                [
                    new MemoryBackendCapability
                    {
                        Mode = MemoryBackendMode.Legacy,
                        Support = MemoryCapabilitySupportLevel.Conditional,
                        Invasiveness = MemoryObservationInvasiveness.OutOfProcess,
                        PrecisionClass = MemoryObservationPrecisionClass.SampledFinalValue,
                        LatencyClass = MemoryObservationLatencyClass.OverOrEqual100Milliseconds,
                        IsConfiguredDefault = settings.MemoryWatcher.Backend == MemoryBackendMode.Legacy,
                        Reason = "Attach to a running PCSX2 process to validate the legacy reader.",
                    },
                    new MemoryBackendCapability
                    {
                        Mode = MemoryBackendMode.MemoryWatcher,
                        Support = MemoryCapabilitySupportLevel.Supported,
                        Invasiveness = MemoryObservationInvasiveness.OperatingSystemHook,
                        PrecisionClass = MemoryObservationPrecisionClass.TransientEdgeExact,
                        LatencyClass = MemoryObservationLatencyClass.Under1Millisecond,
                        IsConfiguredDefault = settings.MemoryWatcher.Backend == MemoryBackendMode.MemoryWatcher,
                    },
                ],
            };
        }
    }

    private sealed class DetachedFakeMemoryBackendCapabilityService : IMemoryBackendCapabilityService
    {
        public MemoryBackendCapabilityReport Inspect(
            IGameClient? gameClient,
            OutbreakTrackerSettings? settingsOverride = null
        )
        {
            OutbreakTrackerSettings settings = settingsOverride ?? new OutbreakTrackerSettings();
            return new MemoryBackendCapabilityReport
            {
                Host = new MemoryWatchHostEnvironment
                {
                    OperatingSystem = "Windows Test",
                    RuntimeDescription = ".NET Test",
                    ProcessArchitecture = System.Runtime.InteropServices.Architecture.X64,
                    Is64BitProcess = true,
                    UserName = "tester",
                    IsElevatedUser = false,
                    SupportsPackagedRemoteAot = true,
                    SupportsSoftDirtyTracking = false,
                    SupportsDebuggerMediatedHardwareWatchpoints = true,
                },
                MemoryWatcher = new MemoryWatchCapabilityNegotiationResult
                {
                    Host = new MemoryWatchHostEnvironment
                    {
                        OperatingSystem = "Windows Test",
                        RuntimeDescription = ".NET Test",
                        ProcessArchitecture = System.Runtime.InteropServices.Architecture.X64,
                        Is64BitProcess = true,
                        UserName = "tester",
                        IsElevatedUser = false,
                        SupportsPackagedRemoteAot = true,
                        SupportsSoftDirtyTracking = false,
                        SupportsDebuggerMediatedHardwareWatchpoints = true,
                    },
                    Target = new MemoryWatchTargetEnvironment
                    {
                        ProcessId = 0,
                        ProcessName = string.Empty,
                        ProcessFound = false,
                        SessionOpened = false,
                        SessionFailureReason = "No running PCSX2 process is attached.",
                    },
                    Capabilities =
                    [
                        new MemoryWatchNegotiatedCapability
                        {
                            Backend = WatchBackendKind.Snapshot,
                            BackendName = "snapshot-bit-diff",
                            Invasiveness = MemoryObservationInvasiveness.OutOfProcess,
                            PrecisionClass = MemoryObservationPrecisionClass.SampledFinalValue,
                            LatencyClass = MemoryObservationLatencyClass.UnknownOrCallerDriven,
                            EnvironmentSupport = MemoryCapabilitySupportLevel.Conditional,
                            EnvironmentConstraintKind = MemoryCapabilityConstraintKind.TargetProcessMissing,
                            EnvironmentSupportReason =
                                "Attach to a running PCSX2 process to validate the plain external reader.",
                            CurrentCapability = null,
                            CurrentRequestAvailable = false,
                            CurrentRequestConstraintKind = MemoryCapabilityConstraintKind.TargetProcessMissing,
                            CurrentRequestReason = "No running PCSX2 process is attached.",
                        },
                        new MemoryWatchNegotiatedCapability
                        {
                            Backend = WatchBackendKind.HashIndexedSnapshot,
                            BackendName = "hash-indexed-snapshot",
                            Invasiveness = MemoryObservationInvasiveness.OutOfProcess,
                            PrecisionClass = MemoryObservationPrecisionClass.SampledFinalValue,
                            LatencyClass = MemoryObservationLatencyClass.UnknownOrCallerDriven,
                            EnvironmentSupport = MemoryCapabilitySupportLevel.Conditional,
                            EnvironmentConstraintKind = MemoryCapabilityConstraintKind.TargetProcessMissing,
                            EnvironmentSupportReason =
                                "Attach to a running PCSX2 process to validate hash-indexed grouped snapshot reads.",
                            CurrentCapability = null,
                            CurrentRequestAvailable = false,
                            CurrentRequestConstraintKind = MemoryCapabilityConstraintKind.TargetProcessMissing,
                            CurrentRequestReason = "No running PCSX2 process is attached.",
                        },
                        new MemoryWatchNegotiatedCapability
                        {
                            Backend = WatchBackendKind.SegmentedSnapshot,
                            BackendName = "segmented-snapshot",
                            Invasiveness = MemoryObservationInvasiveness.OutOfProcess,
                            PrecisionClass = MemoryObservationPrecisionClass.SampledFinalValue,
                            LatencyClass = MemoryObservationLatencyClass.UnknownOrCallerDriven,
                            EnvironmentSupport = MemoryCapabilitySupportLevel.Conditional,
                            EnvironmentConstraintKind = MemoryCapabilityConstraintKind.TargetProcessMissing,
                            EnvironmentSupportReason =
                                "Attach to a running PCSX2 process to validate segmented grouped snapshot reads.",
                            CurrentCapability = null,
                            CurrentRequestAvailable = false,
                            CurrentRequestConstraintKind = MemoryCapabilityConstraintKind.TargetProcessMissing,
                            CurrentRequestReason = "No running PCSX2 process is attached.",
                        },
                        new MemoryWatchNegotiatedCapability
                        {
                            Backend = WatchBackendKind.DirtyRange,
                            BackendName = "dirty-range-then-diff",
                            Invasiveness = MemoryObservationInvasiveness.ExecutableHook,
                            PrecisionClass = MemoryObservationPrecisionClass.SignaledFinalValue,
                            LatencyClass = MemoryObservationLatencyClass.Under1Millisecond,
                            EnvironmentSupport = MemoryCapabilitySupportLevel.Conditional,
                            EnvironmentConstraintKind = MemoryCapabilityConstraintKind.RequiresCooperativeProducer,
                            EnvironmentSupportReason =
                                "Dirty-range observation requires a cooperative producer or in-process helper that can publish changed spans.",
                            CurrentCapability = null,
                            CurrentRequestAvailable = false,
                            CurrentRequestConstraintKind = MemoryCapabilityConstraintKind.TargetProcessMissing,
                            CurrentRequestReason = "No running PCSX2 process is attached.",
                        },
                        new MemoryWatchNegotiatedCapability
                        {
                            Backend = WatchBackendKind.DirtyPage,
                            BackendName = "dirty-page-then-diff",
                            Invasiveness = MemoryObservationInvasiveness.OperatingSystemHook,
                            PrecisionClass = MemoryObservationPrecisionClass.SignaledFinalValue,
                            LatencyClass = MemoryObservationLatencyClass.Under1Millisecond,
                            EnvironmentSupport = MemoryCapabilitySupportLevel.Conditional,
                            EnvironmentConstraintKind = MemoryCapabilityConstraintKind.RequiresPageTracker,
                            EnvironmentSupportReason =
                                "Dirty-page observation needs page markers or another OS-mediated producer path beyond a plain external reader.",
                            CurrentCapability = null,
                            CurrentRequestAvailable = false,
                            CurrentRequestConstraintKind = MemoryCapabilityConstraintKind.TargetProcessMissing,
                            CurrentRequestReason = "No running PCSX2 process is attached.",
                        },
                        new MemoryWatchNegotiatedCapability
                        {
                            Backend = WatchBackendKind.PageFault,
                            BackendName = "page-fault-then-diff",
                            Invasiveness = MemoryObservationInvasiveness.OperatingSystemHook,
                            PrecisionClass = MemoryObservationPrecisionClass.SignaledFinalValue,
                            LatencyClass = MemoryObservationLatencyClass.Under1Millisecond,
                            EnvironmentSupport = MemoryCapabilitySupportLevel.Conditional,
                            EnvironmentConstraintKind = MemoryCapabilityConstraintKind.TargetProcessMissing,
                            EnvironmentSupportReason =
                                "Attach to a running Windows PCSX2 process and enable intrusive backends to validate debugger-mediated PAGE_GUARD wakeups.",
                            CurrentCapability = null,
                            CurrentRequestAvailable = false,
                            CurrentRequestConstraintKind = MemoryCapabilityConstraintKind.TargetProcessMissing,
                            CurrentRequestReason = "No running PCSX2 process is attached.",
                        },
                        new MemoryWatchNegotiatedCapability
                        {
                            Backend = WatchBackendKind.HardwareWatchpoint,
                            BackendName = "hardware-watchpoint",
                            Invasiveness = MemoryObservationInvasiveness.OperatingSystemHook,
                            PrecisionClass = MemoryObservationPrecisionClass.TransientEdgeExact,
                            LatencyClass = MemoryObservationLatencyClass.Under1Millisecond,
                            EnvironmentSupport = MemoryCapabilitySupportLevel.Conditional,
                            EnvironmentConstraintKind = MemoryCapabilityConstraintKind.TargetProcessMissing,
                            EnvironmentSupportReason =
                                "Attach to a running PCSX2 process and provide explicit target thread ids to fully negotiate per-thread watchpoint arming.",
                            CurrentCapability = null,
                            CurrentRequestAvailable = false,
                            CurrentRequestConstraintKind = MemoryCapabilityConstraintKind.TargetProcessMissing,
                            CurrentRequestReason = "No running PCSX2 process is attached.",
                        },
                        new MemoryWatchNegotiatedCapability
                        {
                            Backend = WatchBackendKind.NativeAgent,
                            BackendName = "native-agent",
                            Invasiveness = MemoryObservationInvasiveness.ExecutableHook,
                            PrecisionClass = MemoryObservationPrecisionClass.SignaledFinalValue,
                            LatencyClass = MemoryObservationLatencyClass.Under1Millisecond,
                            EnvironmentSupport = MemoryCapabilitySupportLevel.Conditional,
                            EnvironmentConstraintKind = MemoryCapabilityConstraintKind.RequiresAgent,
                            EnvironmentSupportReason =
                                "Native-agent observation requires loading a cooperative helper inside the target process.",
                            CurrentCapability = null,
                            CurrentRequestAvailable = false,
                            CurrentRequestConstraintKind = MemoryCapabilityConstraintKind.TargetProcessMissing,
                            CurrentRequestReason = "No running PCSX2 process is attached.",
                        },
                    ],
                },
                Backends =
                [
                    new MemoryBackendCapability
                    {
                        Mode = MemoryBackendMode.Legacy,
                        Support = MemoryCapabilitySupportLevel.Conditional,
                        Invasiveness = MemoryObservationInvasiveness.OutOfProcess,
                        PrecisionClass = MemoryObservationPrecisionClass.SampledFinalValue,
                        LatencyClass = MemoryObservationLatencyClass.OverOrEqual100Milliseconds,
                        IsConfiguredDefault = settings.MemoryWatcher.Backend == MemoryBackendMode.Legacy,
                        Reason = "Attach to a running PCSX2 process to validate the legacy reader.",
                    },
                    new MemoryBackendCapability
                    {
                        Mode = MemoryBackendMode.MemoryWatcher,
                        Support = MemoryCapabilitySupportLevel.Conditional,
                        Invasiveness = MemoryObservationInvasiveness.OutOfProcess,
                        PrecisionClass = MemoryObservationPrecisionClass.SampledFinalValue,
                        LatencyClass = MemoryObservationLatencyClass.OverOrEqual100Milliseconds,
                        IsConfiguredDefault = settings.MemoryWatcher.Backend == MemoryBackendMode.MemoryWatcher,
                        Reason = "Attach a live PCSX2 process to validate the requested MemoryWatcher path.",
                    },
                ],
            };
        }
    }

    private sealed class FakeProcessLauncher : IProcessLauncher
    {
        public Observable<ProcessModel> ProcessUpdate => Observable.Empty<ProcessModel>();

        public Observable<bool> IsCancelling => Observable.Empty<bool>();

        public Process? ClientMonitoredProcess => null;

        public IGameClient? AttachedGameClient => null;

        public Task<IGameClient> LaunchAsync(
            string fileName,
            string? arguments,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();

        public Task<IGameClient> AttachAsync(int processId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task TerminateAsync(int? processId = null) => Task.CompletedTask;

        public Task KillAsync(int processId) => Task.CompletedTask;

        public Observable<string> GetErrorObservable() => Observable.Empty<string>();

        public bool HasExited(int processId) => false;

        public int GetExitCode(int processId) => 0;

        public IGameClient? GetActiveGameClient() => null;
    }
}
