using System.Collections.Immutable;
using System.Diagnostics;
using Avalonia.Controls.Notifications;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastEnumUtility;
using MemoryWatcher;
using MemoryWatcher.Remote;
using OutbreakTracker2.Application.Services.Capabilities;
using OutbreakTracker2.Application.Services.Launcher;
using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.MemoryWatcherIntegration;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Utility;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using RemoteConstraintKind = MemoryWatcher.MemoryCapabilityConstraintKind;

namespace OutbreakTracker2.Application.Views.Settings;

internal sealed partial class AppSettingsDialogViewModel : ObservableObject
{
    private static readonly IBrush ReadyBadgeBackgroundBrush = new SolidColorBrush(Color.FromRgb(31, 70, 49));
    private static readonly IBrush ReadyBadgeBorderBrush = new SolidColorBrush(Color.FromRgb(101, 211, 155));
    private static readonly IBrush ReadyBadgeForegroundBrush = new SolidColorBrush(Color.FromRgb(226, 255, 237));

    private static readonly IBrush NeedsWorkBadgeBackgroundBrush = new SolidColorBrush(Color.FromRgb(28, 56, 92));
    private static readonly IBrush NeedsWorkBadgeBorderBrush = new SolidColorBrush(Color.FromRgb(104, 170, 255));
    private static readonly IBrush NeedsWorkBadgeForegroundBrush = new SolidColorBrush(Color.FromRgb(224, 240, 255));

    private static readonly IBrush UnsupportedBadgeBackgroundBrush = new SolidColorBrush(Color.FromRgb(86, 30, 30));
    private static readonly IBrush UnsupportedBadgeBorderBrush = new SolidColorBrush(Color.FromRgb(236, 112, 112));
    private static readonly IBrush UnsupportedBadgeForegroundBrush = new SolidColorBrush(Color.FromRgb(255, 231, 231));

    private static readonly string[] LobbyScenarioOptionValues = CreateLobbyScenarioOptions();
    private static readonly MemoryWatcherBackendOption[] PreferredMemoryWatcherBackendOptionValues =
        CreatePreferredMemoryWatcherBackendOptions();
    private static readonly MemoryWatcherPrecisionOption[] PreferredMemoryWatcherPrecisionOptionValues =
        CreatePreferredMemoryWatcherPrecisionOptions();

    private readonly IAppSettingsService _settingsService;
    private readonly IMemoryBackendCapabilityService _memoryBackendCapabilityService;
    private readonly IProcessLauncher _processLauncher;
    private readonly ISukiToastManager _toastManager;
    private readonly ISukiDialog _dialog;
    private bool _isLoadingSettings;
    private MemoryBackendCapabilityReport? _memoryCapabilityReport;

    public string UserSettingsPath { get; }

    public IReadOnlyList<string> LobbyScenarioOptions { get; } = LobbyScenarioOptionValues;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedMemoryWatcherPreferredBackendOption))]
    [NotifyPropertyChangedFor(nameof(SelectedMemoryWatcherPreferredBackendStateSummary))]
    private IReadOnlyList<MemoryWatcherBackendOption> _preferredMemoryWatcherBackendOptions =
        PreferredMemoryWatcherBackendOptionValues;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedMemoryWatcherPreferredPrecisionOption))]
    [NotifyPropertyChangedFor(nameof(SelectedMemoryWatcherPreferredPrecisionStateSummary))]
    private IReadOnlyList<MemoryWatcherPrecisionOption> _preferredMemoryWatcherPrecisionOptions =
        PreferredMemoryWatcherPrecisionOptionValues;

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string? _validationMessage;

    [ObservableProperty]
    private bool _enableToastAlerts;

    [ObservableProperty]
    private bool _showGameplayUiDuringTransitions;

    [ObservableProperty]
    private bool _playerDangerCondition;

    [ObservableProperty]
    private bool _playerGasCondition;

    [ObservableProperty]
    private bool _playerDeadStatus;

    [ObservableProperty]
    private bool _playerZombieStatus;

    [ObservableProperty]
    private bool _playerDownStatus;

    [ObservableProperty]
    private bool _playerBleedStatus;

    [ObservableProperty]
    private bool _playerHealthZero;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _playerVirusWarningEnabled;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private double _playerVirusWarningThreshold;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _playerVirusCriticalEnabled;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private double _playerVirusCriticalThreshold;

    [ObservableProperty]
    private bool _playerAntiVirusExpired;

    [ObservableProperty]
    private bool _playerAntiVirusGExpired;

    [ObservableProperty]
    private bool _playerBleedStopped;

    [ObservableProperty]
    private bool _playerRoomChange;

    [ObservableProperty]
    private bool _playerJoined;

    [ObservableProperty]
    private bool _playerLeft;

    [ObservableProperty]
    private bool _enemySpawned;

    [ObservableProperty]
    private bool _enemyKilled;

    [ObservableProperty]
    private bool _enemyDespawned;

    [ObservableProperty]
    private bool _enemyRoomChange;

    [ObservableProperty]
    private bool _entitiesDockOnlyShowCurrentPlayerRoom;

    [ObservableProperty]
    private bool _scenarioItemsDockOnlyShowCurrentPlayerRoom;

    [ObservableProperty]
    private bool _scenarioItemsProjectAllOntoMap;

    [ObservableProperty]
    private bool _doorFlagChanged;

    [ObservableProperty]
    private bool _doorDestroyed;

    [ObservableProperty]
    private bool _doorStatusChanged;

    [ObservableProperty]
    private bool _lobbyGameCreated;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _lobbyNameMatchCreated;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _lobbyNameMatchFilter = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _lobbyScenarioMatchCreated;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _lobbyScenarioMatchFilter = string.Empty;

    [ObservableProperty]
    private bool _generateRunReports;

    [ObservableProperty]
    private string _reportOutputDirectory = string.Empty;

    [ObservableProperty]
    private bool _reportWriteMarkdown;

    [ObservableProperty]
    private bool _reportWriteCsv;

    [ObservableProperty]
    private bool _reportWriteHtml;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private int _dataManagerFastUpdateIntervalMs;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private int _dataManagerSlowUpdateIntervalMs;

    [ObservableProperty]
    private MemoryBackendMode _memoryWatcherBackend;

    [ObservableProperty]
    private WatchBackendKind _memoryWatcherPreferredBackend;

    [ObservableProperty]
    private WatchPrecision _memoryWatcherPreferredPrecision;

    [ObservableProperty]
    private bool _memoryWatcherAllowFallback;

    [ObservableProperty]
    private string _memoryWatcherNativeLibraryPath = string.Empty;

    [ObservableProperty]
    private bool _memoryWatcherAllowIntrusiveBackends;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private int _memoryWatcherEventBufferCapacity;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private int _memoryWatcherHashBlockSizeBytes;

    [ObservableProperty]
    private bool _memoryWatcherUseHashIndex;

    public bool IsMemoryWatcherBackendSelected => MemoryWatcherBackend == MemoryBackendMode.MemoryWatcher;

    public bool CanEditMemoryWatcherPreferredPrecision => MemoryWatcherPreferredBackend == WatchBackendKind.Auto;

    public bool IsMemoryWatcherPreferredPrecisionLocked => !CanEditMemoryWatcherPreferredPrecision;

    public MemoryWatcherBackendOption? SelectedMemoryWatcherPreferredBackendOption
    {
        get =>
            PreferredMemoryWatcherBackendOptions.FirstOrDefault(option =>
                option.Value == MemoryWatcherPreferredBackend
            );
        set
        {
            if (value is not null)
            {
                MemoryWatcherPreferredBackend = value.Value;
            }
        }
    }

    public string SelectedMemoryWatcherPreferredBackendStateSummary =>
        SelectedMemoryWatcherPreferredBackendOption?.StateSummary ?? string.Empty;

    public MemoryWatcherPrecisionOption? SelectedMemoryWatcherPreferredPrecisionOption
    {
        get =>
            PreferredMemoryWatcherPrecisionOptions.FirstOrDefault(option =>
                option.Value == MemoryWatcherPreferredPrecision
            );
        set
        {
            if (value is not null)
            {
                MemoryWatcherPreferredPrecision = value.Value;
            }
        }
    }

    public string SelectedMemoryWatcherPreferredPrecisionStateSummary =>
        SelectedMemoryWatcherPreferredPrecisionOption?.StateSummary ?? string.Empty;

    [ObservableProperty]
    private IReadOnlyList<MemoryBackendCard> _backendCapabilityCards = [];

    [ObservableProperty]
    private IReadOnlyList<MemoryWatcherCapabilityCard> _memoryWatcherCapabilityCards = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasReadyMemoryWatcherCapabilityCards))]
    private IReadOnlyList<MemoryWatcherCapabilityCard> _readyMemoryWatcherCapabilityCards = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActionableMemoryWatcherCapabilityCards))]
    private IReadOnlyList<MemoryWatcherCapabilityCard> _actionableMemoryWatcherCapabilityCards = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFutureMemoryWatcherCapabilityCards))]
    private IReadOnlyList<MemoryWatcherCapabilityCard> _futureMemoryWatcherCapabilityCards = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUnavailableMemoryWatcherCapabilityCards))]
    private IReadOnlyList<MemoryWatcherCapabilityCard> _unavailableMemoryWatcherCapabilityCards = [];

    [ObservableProperty]
    private string _memoryCapabilityHostSummary = "Capability scan unavailable.";

    [ObservableProperty]
    private string _memoryCapabilityTargetSummary =
        "Open the settings dialog while OT2 is attached to PCSX2 for a live scan.";

    [ObservableProperty]
    private string _memoryCapabilityNativeLibrarySummary = "Using the packaged MemoryWatcher native runtime.";

    [ObservableProperty]
    private string _memoryCapabilityRealitySummary =
        "Run a capability scan to see which strategies are ready in OT2 now, which are realistic one-step upgrades, which belong to a helper-based deployment class, and which are genuinely blocked by the current host or runtime.";

    [ObservableProperty]
    private string _memoryWatcherPolicySummary =
        "Choose a backend strategy to see both what this host can execute and how OT2's grouped reader will use that strategy today.";

    [ObservableProperty]
    private string _memoryRecommendedConfigurationSummary =
        "Run a capability scan to see the safest default OT2 can use on this machine.";

    [ObservableProperty]
    private string _memoryWatcherRequestPolicyScopeSummary =
        "This picker only exposes request choices that OT2 can safely issue in its current deployment class. Non-snapshot choices still keep OT2's grouped snapshot reads; they change the wake or invalidation path layered on top. Helper-only, Linux-only, runtime-missing, and deeper-integration paths stay visible below as strategy cards for reference.";

    [ObservableProperty]
    private string _memoryCadenceSummary =
        "Cadence summary unavailable until OT2 can compare the draft settings against a Memory capability scan.";

    [ObservableProperty]
    private string _memorySupportLegendSummary =
        "Green - Ready now: OT2 can use it immediately."
        + "\nBlue - Action needed: the host path exists, but OT2 still needs something specific such as a live target, opt-in, probe retry, helper path, or deeper watch-model integration."
        + "\nRed - Unavailable here: the operating system, runtime, packaged backend, or current rights cannot expose it here.";

    [ObservableProperty]
    private string _readyMemoryWatcherCapabilitySummary = "These are safe to choose now on the attached PCSX2 target.";

    [ObservableProperty]
    private string _actionableMemoryWatcherCapabilitySummary =
        "These can be enabled from OT2 today, but they still need one explicit step such as intrusive opt-in or a clean live probe.";

    [ObservableProperty]
    private string _futureMemoryWatcherCapabilitySummary =
        "These are real strategies, but they currently belong to a helper-based or deeper-integration deployment class rather than OT2's plain external grouped reader.";

    [ObservableProperty]
    private string _unavailableMemoryWatcherCapabilitySummary =
        "These are blocked by the current host, runtime, packaged backend, or rights.";

    public bool HasReadyMemoryWatcherCapabilityCards => ReadyMemoryWatcherCapabilityCards.Count > 0;

    public bool HasActionableMemoryWatcherCapabilityCards => ActionableMemoryWatcherCapabilityCards.Count > 0;

    public bool HasFutureMemoryWatcherCapabilityCards => FutureMemoryWatcherCapabilityCards.Count > 0;

    public bool HasUnavailableMemoryWatcherCapabilityCards => UnavailableMemoryWatcherCapabilityCards.Count > 0;

    public AppSettingsDialogViewModel(
        IAppSettingsService settingsService,
        IMemoryBackendCapabilityService memoryBackendCapabilityService,
        IProcessLauncher processLauncher,
        ISukiToastManager toastManager,
        ISukiDialog dialog
    )
    {
        _settingsService = settingsService;
        _memoryBackendCapabilityService = memoryBackendCapabilityService;
        _processLauncher = processLauncher;
        _toastManager = toastManager;
        _dialog = dialog;

        UserSettingsPath = settingsService.UserSettingsPath;

        Load(settingsService.Current);
        RefreshMemoryCapabilities();
        UpdateValidationMessage();
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        try
        {
            OutbreakTrackerSettings settings = BuildSettings();
            if (!TryValidateSettings(settings, out string? error))
            {
                ValidationMessage = error;
                return;
            }

            await _settingsService.SaveAsync(settings).ConfigureAwait(true);

            QueueToast(NotificationType.Information, "Settings Saved", "Tracker settings were saved and reloaded.");

            _dialog.Dismiss();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            QueueToast(NotificationType.Error, "Save Failed", ex.Message);
        }
    }

    [RelayCommand]
    private void Cancel() => _dialog.Dismiss();

    [RelayCommand]
    private async Task ResetToDefaultsAsync()
    {
        try
        {
            OutbreakTrackerSettings settings = await _settingsService.ResetToDefaultsAsync().ConfigureAwait(true);
            Load(settings);
            UpdateValidationMessage();
            QueueToast(
                NotificationType.Information,
                "Settings Reset",
                "User overrides were removed and bundled defaults are now active."
            );
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            QueueToast(NotificationType.Error, "Reset Failed", ex.Message);
        }
    }

    [RelayCommand]
    private void OpenSettingsFolder()
    {
        try
        {
            string? directoryPath = Path.GetDirectoryName(UserSettingsPath);
            if (string.IsNullOrWhiteSpace(directoryPath))
                return;

            Process.Start(new ProcessStartInfo(directoryPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            QueueToast(NotificationType.Error, "Open Folder Failed", ex.Message);
        }
    }

    [RelayCommand]
    private void RefreshMemoryCapabilities()
    {
        RefreshMemoryCapabilitiesCore(showErrors: true);
    }

    [RelayCommand]
    private void UseAppBackend(MemoryBackendMode mode)
    {
        MemoryWatcherBackend = mode;
        ApplyMemoryCapabilityPresentation();
    }

    [RelayCommand]
    private void UseMemoryWatcherCapability(MemoryWatcherCapabilityCard? capability)
    {
        if (capability is null || !capability.CanApply)
        {
            return;
        }

        MemoryWatcherBackend = MemoryBackendMode.MemoryWatcher;
        MemoryWatcherPreferredBackend = capability.Backend;
        MemoryWatcherPreferredPrecision = capability.Precision;
        MemoryWatcherAllowFallback = capability.ConstraintKind == MemoryCapabilityConstraintKind.NeedsAttachedTarget;
        ApplyCapabilityPrerequisites(capability.Backend);
        ApplyMemoryCapabilityPresentation();
    }

    [RelayCommand]
    private void UseRecommendedMemoryConfiguration()
    {
        if (_memoryCapabilityReport is null)
        {
            return;
        }

        MemoryBackendMode recommendedBackend = SelectRecommendedBackendMode(_memoryCapabilityReport);
        MemoryWatcherBackend = recommendedBackend;

        if (
            recommendedBackend == MemoryBackendMode.MemoryWatcher
            && _memoryCapabilityReport.MemoryWatcher.Capabilities.Count > 0
        )
        {
            MemoryWatcherPreferredBackend = WatchBackendKind.Auto;
            MemoryWatcherPreferredPrecision = WatchPrecision.SnapshotBitExact;
            MemoryWatcherAllowFallback = true;
            MemoryWatcherAllowIntrusiveBackends = false;
        }

        ApplyMemoryCapabilityPresentation();
    }

    internal async Task ExportAsync(Stream destination, CancellationToken cancellationToken = default)
    {
        try
        {
            await _settingsService.ExportAsync(destination, cancellationToken).ConfigureAwait(true);
            QueueToast(NotificationType.Information, "Settings Exported", "Current tracker settings were exported.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            QueueToast(NotificationType.Error, "Export Failed", ex.Message);
        }
    }

    internal async Task ImportAsync(Stream source, CancellationToken cancellationToken = default)
    {
        try
        {
            OutbreakTrackerSettings settings = await _settingsService
                .ImportAsync(source, cancellationToken)
                .ConfigureAwait(true);
            Load(settings);
            UpdateValidationMessage();
            QueueToast(
                NotificationType.Information,
                "Settings Imported",
                "Tracker settings were imported and reloaded."
            );
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            QueueToast(NotificationType.Error, "Import Failed", ex.Message);
        }
    }

    internal void NotifyFileDialogUnavailable(string operation) =>
        QueueToast(NotificationType.Error, $"{operation} Unavailable", "This window could not open a file dialog.");

    partial void OnPlayerVirusWarningThresholdChanged(double value) => UpdateValidationMessage();

    partial void OnPlayerVirusCriticalThresholdChanged(double value) => UpdateValidationMessage();

    partial void OnLobbyNameMatchCreatedChanged(bool value) => UpdateValidationMessage();

    partial void OnLobbyNameMatchFilterChanged(string value) => UpdateValidationMessage();

    partial void OnLobbyScenarioMatchCreatedChanged(bool value) => UpdateValidationMessage();

    partial void OnLobbyScenarioMatchFilterChanged(string value) => UpdateValidationMessage();

    partial void OnGenerateRunReportsChanged(bool value) => UpdateValidationMessage();

    partial void OnReportWriteMarkdownChanged(bool value) => UpdateValidationMessage();

    partial void OnReportWriteCsvChanged(bool value) => UpdateValidationMessage();

    partial void OnReportWriteHtmlChanged(bool value) => UpdateValidationMessage();

    partial void OnDataManagerFastUpdateIntervalMsChanged(int value)
    {
        UpdateValidationMessage();
        ApplyMemoryCapabilityPresentation();
    }

    partial void OnDataManagerSlowUpdateIntervalMsChanged(int value)
    {
        UpdateValidationMessage();
        ApplyMemoryCapabilityPresentation();
    }

    partial void OnMemoryWatcherBackendChanged(MemoryBackendMode value)
    {
        OnPropertyChanged(nameof(IsMemoryWatcherBackendSelected));
        ApplyMemoryCapabilityPresentation();
    }

    partial void OnMemoryWatcherPreferredBackendChanged(WatchBackendKind value)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        ApplyCapabilityPrerequisites(value);

        if (TryGetFixedPrecisionForBackend(value, out WatchPrecision precision))
        {
            MemoryWatcherPreferredPrecision = precision;
        }

        OnPropertyChanged(nameof(CanEditMemoryWatcherPreferredPrecision));
        OnPropertyChanged(nameof(IsMemoryWatcherPreferredPrecisionLocked));
        OnPropertyChanged(nameof(SelectedMemoryWatcherPreferredBackendOption));
        OnPropertyChanged(nameof(SelectedMemoryWatcherPreferredBackendStateSummary));
        OnPropertyChanged(nameof(SelectedMemoryWatcherPreferredPrecisionOption));
        UpdateValidationMessage();
        RefreshMemoryCapabilitiesCore();
    }

    partial void OnMemoryWatcherPreferredPrecisionChanged(WatchPrecision value)
    {
        OnPropertyChanged(nameof(SelectedMemoryWatcherPreferredPrecisionOption));
        OnPropertyChanged(nameof(SelectedMemoryWatcherPreferredPrecisionStateSummary));
        UpdateValidationMessage();
        RefreshMemoryCapabilitiesCore();
    }

    partial void OnMemoryWatcherAllowFallbackChanged(bool value)
    {
        UpdateValidationMessage();
        RefreshMemoryCapabilitiesCore();
    }

    partial void OnMemoryWatcherNativeLibraryPathChanged(string value)
    {
        UpdateValidationMessage();
        RefreshMemoryCapabilitiesCore();
    }

    partial void OnMemoryWatcherAllowIntrusiveBackendsChanged(bool value)
    {
        UpdateValidationMessage();
        RefreshMemoryCapabilitiesCore();
    }

    partial void OnMemoryWatcherEventBufferCapacityChanged(int value)
    {
        UpdateValidationMessage();
        RefreshMemoryCapabilitiesCore();
    }

    partial void OnMemoryWatcherHashBlockSizeBytesChanged(int value)
    {
        UpdateValidationMessage();
        RefreshMemoryCapabilitiesCore();
    }

    partial void OnMemoryWatcherUseHashIndexChanged(bool value)
    {
        UpdateValidationMessage();
        RefreshMemoryCapabilitiesCore();
    }

    private bool CanSave() => string.IsNullOrWhiteSpace(ValidationMessage);

    private void Load(OutbreakTrackerSettings settings)
    {
        _isLoadingSettings = true;
        try
        {
            NotificationSettings notifications = settings.Notifications ?? new();
            DisplaySettings display = settings.Display ?? new();
            EntitiesDockSettings entitiesDock = display.EntitiesDock ?? new();
            ScenarioItemsDockSettings scenarioItemsDock = display.ScenarioItemsDock ?? new();
            AlertRuleSettings alertRules = settings.AlertRules ?? new();
            PlayerAlertRuleSettings players = alertRules.Players ?? new();
            EnemyAlertRuleSettings enemies = alertRules.Enemies ?? new();
            DoorAlertRuleSettings doors = alertRules.Doors ?? new();
            LobbyAlertRuleSettings lobby = alertRules.Lobby ?? new();
            RunReportSettings runReports = settings.RunReports ?? new();
            DataManagerSettings dataManager = settings.DataManager ?? new();
            MemoryWatcherSettings memoryWatcher = settings.MemoryWatcher ?? new();

            EnableToastAlerts = notifications.EnableToastAlerts;
            ShowGameplayUiDuringTransitions = display.ShowGameplayUiDuringTransitions;

            PlayerDangerCondition = players.DangerCondition;
            PlayerGasCondition = players.GasCondition;
            PlayerDeadStatus = players.DeadStatus;
            PlayerZombieStatus = players.ZombieStatus;
            PlayerDownStatus = players.DownStatus;
            PlayerBleedStatus = players.BleedStatus;
            PlayerHealthZero = players.HealthZero;
            PlayerVirusWarningEnabled = players.VirusWarningEnabled;
            PlayerVirusWarningThreshold = players.VirusWarningThreshold;
            PlayerVirusCriticalEnabled = players.VirusCriticalEnabled;
            PlayerVirusCriticalThreshold = players.VirusCriticalThreshold;
            PlayerAntiVirusExpired = players.AntiVirusExpired;
            PlayerAntiVirusGExpired = players.AntiVirusGExpired;
            PlayerBleedStopped = players.BleedStopped;
            PlayerRoomChange = players.RoomChange;
            PlayerJoined = players.Joined;
            PlayerLeft = players.Left;

            EnemySpawned = enemies.Spawned;
            EnemyKilled = enemies.Killed;
            EnemyDespawned = enemies.Despawned;
            EnemyRoomChange = enemies.RoomChange;
            EntitiesDockOnlyShowCurrentPlayerRoom = entitiesDock.OnlyShowCurrentPlayerRoom;
            ScenarioItemsDockOnlyShowCurrentPlayerRoom = scenarioItemsDock.OnlyShowCurrentPlayerRoom;
            ScenarioItemsProjectAllOntoMap = scenarioItemsDock.ProjectAllOntoMap;

            DoorFlagChanged = doors.FlagChanged;
            DoorDestroyed = doors.Destroyed;
            DoorStatusChanged = doors.StatusChanged;

            LobbyGameCreated = lobby.GameCreated;
            LobbyNameMatchCreated = lobby.NameMatchCreated;
            LobbyNameMatchFilter = lobby.NameMatchFilter ?? string.Empty;
            LobbyScenarioMatchCreated = lobby.ScenarioMatchCreated;
            LobbyScenarioMatchFilter = NormalizeScenarioSelection(lobby.ScenarioMatchFilter);

            GenerateRunReports = runReports.GenerateRunReports;
            ReportOutputDirectory = runReports.OutputDirectory ?? string.Empty;
            ReportWriteMarkdown = runReports.WriteMarkdown;
            ReportWriteCsv = runReports.WriteCsv;
            ReportWriteHtml = runReports.WriteHtml;

            DataManagerFastUpdateIntervalMs = dataManager.FastUpdateIntervalMs;
            DataManagerSlowUpdateIntervalMs = dataManager.SlowUpdateIntervalMs;

            MemoryWatcherBackend = memoryWatcher.Backend;
            MemoryWatcherPreferredBackend = memoryWatcher.PreferredBackend;
            MemoryWatcherPreferredPrecision = memoryWatcher.PreferredPrecision;
            MemoryWatcherAllowFallback = memoryWatcher.AllowFallback;
            MemoryWatcherNativeLibraryPath = memoryWatcher.NativeLibraryPath ?? string.Empty;
            MemoryWatcherAllowIntrusiveBackends = memoryWatcher.AllowIntrusiveBackends;
            MemoryWatcherEventBufferCapacity = memoryWatcher.EventBufferCapacity;
            MemoryWatcherHashBlockSizeBytes = memoryWatcher.HashBlockSizeBytes;
            MemoryWatcherUseHashIndex = memoryWatcher.UseHashIndex;
        }
        finally
        {
            _isLoadingSettings = false;
        }
    }

    private OutbreakTrackerSettings BuildSettings() =>
        new()
        {
            Notifications = new NotificationSettings { EnableToastAlerts = EnableToastAlerts },
            Display = new DisplaySettings
            {
                ShowGameplayUiDuringTransitions = ShowGameplayUiDuringTransitions,
                EntitiesDock = new EntitiesDockSettings
                {
                    OnlyShowCurrentPlayerRoom = EntitiesDockOnlyShowCurrentPlayerRoom,
                },
                ScenarioItemsDock = new ScenarioItemsDockSettings
                {
                    OnlyShowCurrentPlayerRoom = ScenarioItemsDockOnlyShowCurrentPlayerRoom,
                    ProjectAllOntoMap = ScenarioItemsProjectAllOntoMap,
                },
            },
            AlertRules = new AlertRuleSettings
            {
                Players = new PlayerAlertRuleSettings
                {
                    DangerCondition = PlayerDangerCondition,
                    GasCondition = PlayerGasCondition,
                    DeadStatus = PlayerDeadStatus,
                    ZombieStatus = PlayerZombieStatus,
                    DownStatus = PlayerDownStatus,
                    BleedStatus = PlayerBleedStatus,
                    HealthZero = PlayerHealthZero,
                    VirusWarningEnabled = PlayerVirusWarningEnabled,
                    VirusWarningThreshold = PlayerVirusWarningThreshold,
                    VirusCriticalEnabled = PlayerVirusCriticalEnabled,
                    VirusCriticalThreshold = PlayerVirusCriticalThreshold,
                    AntiVirusExpired = PlayerAntiVirusExpired,
                    AntiVirusGExpired = PlayerAntiVirusGExpired,
                    BleedStopped = PlayerBleedStopped,
                    RoomChange = PlayerRoomChange,
                    Joined = PlayerJoined,
                    Left = PlayerLeft,
                },
                Enemies = new EnemyAlertRuleSettings
                {
                    Spawned = EnemySpawned,
                    Killed = EnemyKilled,
                    Despawned = EnemyDespawned,
                    RoomChange = EnemyRoomChange,
                },
                Doors = new DoorAlertRuleSettings
                {
                    FlagChanged = DoorFlagChanged,
                    Destroyed = DoorDestroyed,
                    StatusChanged = DoorStatusChanged,
                },
                Lobby = new LobbyAlertRuleSettings
                {
                    GameCreated = LobbyGameCreated,
                    NameMatchCreated = LobbyNameMatchCreated,
                    NameMatchFilter = NormalizeFilter(LobbyNameMatchFilter),
                    ScenarioMatchCreated = LobbyScenarioMatchCreated,
                    ScenarioMatchFilter = NormalizeScenarioSelection(LobbyScenarioMatchFilter),
                },
            },
            RunReports = new RunReportSettings
            {
                GenerateRunReports = GenerateRunReports,
                OutputDirectory = NormalizeFilter(ReportOutputDirectory),
                WriteMarkdown = ReportWriteMarkdown,
                WriteCsv = ReportWriteCsv,
                WriteHtml = ReportWriteHtml,
            },
            DataManager = new DataManagerSettings
            {
                FastUpdateIntervalMs = DataManagerFastUpdateIntervalMs,
                SlowUpdateIntervalMs = DataManagerSlowUpdateIntervalMs,
            },
            MemoryWatcher = new MemoryWatcherSettings
            {
                Backend = MemoryWatcherBackend,
                PreferredBackend = MemoryWatcherPreferredBackend,
                PreferredPrecision = MemoryWatcherPreferredPrecision,
                AllowFallback = MemoryWatcherAllowFallback,
                NativeLibraryPath = NormalizeOptionalText(MemoryWatcherNativeLibraryPath),
                AllowIntrusiveBackends = MemoryWatcherAllowIntrusiveBackends,
                EventBufferCapacity = MemoryWatcherEventBufferCapacity,
                HashBlockSizeBytes = MemoryWatcherHashBlockSizeBytes,
                UseHashIndex = MemoryWatcherUseHashIndex,
            },
        };

    private void RefreshMemoryCapabilitiesCore(bool showErrors = false)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        try
        {
            OutbreakTrackerSettings draftSettings = BuildSettings();
            _memoryCapabilityReport = _memoryBackendCapabilityService.Inspect(
                _processLauncher.GetActiveGameClient(),
                draftSettings
            );
            ApplyMemoryCapabilityPresentation();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            _memoryCapabilityReport = null;
            MemoryCapabilityHostSummary = "Capability scan failed.";
            MemoryCapabilityTargetSummary = ex.Message;
            MemoryCapabilityNativeLibrarySummary = BuildNativeLibrarySummary(BuildSettings().MemoryWatcher);
            PreferredMemoryWatcherBackendOptions = BuildPreferredMemoryWatcherBackendOptions(null, BuildSettings());
            PreferredMemoryWatcherPrecisionOptions = BuildPreferredMemoryWatcherPrecisionOptions(null, BuildSettings());
            MemoryCapabilityRealitySummary =
                "Capability scan failed, so OT2 cannot summarize what is ready versus runtime-missing yet.";
            MemoryWatcherPolicySummary =
                "The draft MemoryWatcher request could not be negotiated. Fix the settings above and refresh the scan.";
            MemoryRecommendedConfigurationSummary =
                "No recommendation is available until the capability scan succeeds.";
            MemoryWatcherRequestPolicyScopeSummary =
                "The Memory scan failed, so OT2 is keeping the request picker conservative until it can classify the current host and target again.";
            MemoryCadenceSummary =
                "Cadence summary unavailable because the Memory scan failed. Until the scan recovers, treat the fast and slow intervals as OT2's only dependable refresh description.";
            BackendCapabilityCards = [];
            MemoryWatcherCapabilityCards = [];
            ReadyMemoryWatcherCapabilityCards = [];
            ActionableMemoryWatcherCapabilityCards = [];
            FutureMemoryWatcherCapabilityCards = [];
            UnavailableMemoryWatcherCapabilityCards = [];

            if (showErrors)
            {
                QueueToast(NotificationType.Error, "Capability Scan Failed", ex.Message);
            }

            UpdateValidationMessage();
        }
    }

    private void ApplyMemoryCapabilityPresentation()
    {
        if (_isLoadingSettings)
        {
            return;
        }

        OutbreakTrackerSettings draftSettings = BuildSettings();
        if (_memoryCapabilityReport is null)
        {
            BackendCapabilityCards = [];
            MemoryWatcherCapabilityCards = [];
            ReadyMemoryWatcherCapabilityCards = [];
            ActionableMemoryWatcherCapabilityCards = [];
            FutureMemoryWatcherCapabilityCards = [];
            UnavailableMemoryWatcherCapabilityCards = [];
            MemoryCapabilityHostSummary = "Capability scan unavailable.";
            MemoryCapabilityTargetSummary = "Capability scan has not been run yet.";
            MemoryCapabilityNativeLibrarySummary = BuildNativeLibrarySummary(draftSettings.MemoryWatcher);
            PreferredMemoryWatcherBackendOptions = BuildPreferredMemoryWatcherBackendOptions(null, draftSettings);
            PreferredMemoryWatcherPrecisionOptions = BuildPreferredMemoryWatcherPrecisionOptions(null, draftSettings);
            MemoryCapabilityRealitySummary =
                "Run a capability scan to see which strategies are ready in OT2 now, which are realistic one-step upgrades, which belong to a helper-based deployment class, and which are genuinely blocked by the current host or runtime.";
            MemoryWatcherPolicySummary =
                "Choose a backend strategy to see how OT2 will ask MemoryWatcher to build its region watches.";
            MemoryRecommendedConfigurationSummary =
                "Run a capability scan to see the safest default OT2 can use on this machine.";
            MemoryWatcherRequestPolicyScopeSummary =
                "Until a Memory scan succeeds, OT2 keeps this picker conservative and only exposes the safe snapshot-family baseline.";
            MemoryCadenceSummary =
                "Without a Memory scan, OT2 can only promise the fast and slow intervals as its refresh cadence. Wait-capable upgrades are classified after the scan succeeds.";
            return;
        }

        MemoryCapabilityHostSummary = BuildHostSummary(_memoryCapabilityReport.Host);
        MemoryCapabilityTargetSummary = BuildTargetSummary(_memoryCapabilityReport.MemoryWatcher.Target);
        MemoryCapabilityNativeLibrarySummary = BuildNativeLibrarySummary(draftSettings.MemoryWatcher);
        PreferredMemoryWatcherBackendOptions = BuildPreferredMemoryWatcherBackendOptions(
            _memoryCapabilityReport,
            draftSettings
        );
        PreferredMemoryWatcherPrecisionOptions = BuildPreferredMemoryWatcherPrecisionOptions(
            _memoryCapabilityReport,
            draftSettings
        );
        MemoryCapabilityRealitySummary = BuildCapabilityRealitySummary(_memoryCapabilityReport.MemoryWatcher);
        BackendCapabilityCards = BuildBackendCapabilityCards(_memoryCapabilityReport, draftSettings);
        MemoryWatcherCapabilityCards = BuildMemoryWatcherCapabilityCards(_memoryCapabilityReport, draftSettings);
        ApplyMemoryWatcherCapabilitySections(MemoryWatcherCapabilityCards);
        MemoryWatcherPolicySummary = BuildMemoryWatcherPolicySummary(_memoryCapabilityReport, draftSettings);
        MemoryRecommendedConfigurationSummary = BuildRecommendedConfigurationSummary(_memoryCapabilityReport);
        MemoryWatcherRequestPolicyScopeSummary = BuildRequestPolicyScopeSummary(_memoryCapabilityReport, draftSettings);
        MemoryCadenceSummary = BuildMemoryCadenceSummary(_memoryCapabilityReport, draftSettings);
        MemorySupportLegendSummary = BuildSupportLegendSummary();
        UpdateValidationMessage();
    }

    private void UpdateValidationMessage()
    {
        if (_isLoadingSettings)
            return;

        OutbreakTrackerSettings settings = BuildSettings();
        ValidationMessage = TryValidateSettings(settings, out string? error) ? null : error;
    }

    private bool TryValidateSettings(OutbreakTrackerSettings settings, out string? error)
    {
        if (!settings.TryValidate(out error))
        {
            return false;
        }

        return TryValidateMemoryCapabilitySelection(settings, out error);
    }

    private bool TryValidateMemoryCapabilitySelection(OutbreakTrackerSettings settings, out string? error)
    {
        if (settings.MemoryWatcher.Backend != MemoryBackendMode.MemoryWatcher)
        {
            error = null;
            return true;
        }

        if (_memoryCapabilityReport is null)
        {
            if (
                settings.MemoryWatcher.PreferredBackend
                is WatchBackendKind.Auto
                    or WatchBackendKind.Snapshot
                    or WatchBackendKind.HashIndexedSnapshot
                    or WatchBackendKind.SegmentedSnapshot
            )
            {
                error = null;
                return true;
            }

            error =
                $"Refresh the Memory scan before saving forced backend '{GetWatchBackendLabel(settings.MemoryWatcher.PreferredBackend)}'. OT2 cannot safely validate that request from the current scan state.";
            return false;
        }

        MemoryWatchNegotiatedCapability? requested = FindRequestedCapability(
            _memoryCapabilityReport.MemoryWatcher.Capabilities,
            settings
        );
        if (requested is null)
        {
            error =
                "The current MemoryWatcher request did not resolve to any capability from the latest scan. Use Automatic or refresh the Memory scan.";
            return false;
        }

        CapabilityConstraintDisplay constraint = DescribeCapabilityConstraint(requested);
        if (requested.CurrentRequestAvailable || constraint.CanApply)
        {
            error = null;
            return true;
        }

        if (constraint.Kind == MemoryCapabilityConstraintKind.NeedsAttachedTarget)
        {
            error = null;
            return true;
        }

        error = BuildUnsafeMemorySelectionMessage(settings, requested, constraint);
        return false;
    }

    private static string BuildUnsafeMemorySelectionMessage(
        OutbreakTrackerSettings settings,
        MemoryWatchNegotiatedCapability requested,
        CapabilityConstraintDisplay constraint
    )
    {
        string backendLabel = GetWatchBackendLabel(requested.Backend);
        string modeHint =
            settings.MemoryWatcher.PreferredBackend == WatchBackendKind.Auto
                ? "current Automatic request"
                : $"forced backend '{backendLabel}'";

        return constraint.Kind switch
        {
            MemoryCapabilityConstraintKind.NeedsOt2Helper =>
                $"OT2 cannot safely save the {modeHint} because {backendLabel} needs a cooperative helper or in-process producer path that this app does not run today.",
            MemoryCapabilityConstraintKind.NeedsThreadScopedIntegration
            or MemoryCapabilityConstraintKind.NotSupportedByOt2Model =>
                $"OT2 cannot safely save the {modeHint} because {backendLabel} still needs a finer point-watch model than the current grouped EEmem reader exposes.",
            MemoryCapabilityConstraintKind.NeedsOt2RuntimeIntegration =>
                $"OT2 cannot safely save the {modeHint} because {backendLabel} is not surfaced by the current OT2/runtime integration path.",
            MemoryCapabilityConstraintKind.NotSupportedByHost when IsPackagedRuntimeSurfaceUnavailable(requested) =>
                $"OT2 cannot safely save the {modeHint} because the packaged runtime on this host does not surface {backendLabel}.",
            MemoryCapabilityConstraintKind.NotSupportedByHost =>
                $"OT2 cannot safely save the {modeHint} because {backendLabel} is unavailable on this host or with the current rights.",
            MemoryCapabilityConstraintKind.NeedsAttachedTarget =>
                $"Attach PCSX2 and refresh the Memory scan before saving the {modeHint}. OT2 cannot verify that request against a live target yet.",
            _ => string.IsNullOrWhiteSpace(constraint.Detail)
                ? $"OT2 cannot safely save the {modeHint} from the latest Memory scan."
                : constraint.Detail,
        };
    }

    private void ApplyCapabilityPrerequisites(WatchBackendKind backend)
    {
        if (RequiresIntrusiveOptIn(backend))
        {
            MemoryWatcherAllowIntrusiveBackends = true;
        }
    }

    private static IReadOnlyList<MemoryWatcherBackendOption> BuildPreferredMemoryWatcherBackendOptions(
        MemoryBackendCapabilityReport? report,
        OutbreakTrackerSettings settings
    )
    {
        List<MemoryWatcherBackendOption> options = new(PreferredMemoryWatcherBackendOptionValues.Length);
        for (int i = 0; i < PreferredMemoryWatcherBackendOptionValues.Length; i++)
        {
            MemoryWatcherBackendOption template = PreferredMemoryWatcherBackendOptionValues[i];
            if (!ShouldExposePreferredBackendOption(template.Value, report))
            {
                continue;
            }

            options.Add(AnnotatePreferredMemoryWatcherBackendOption(template, report, settings));
        }

        EnsureCurrentPreferredBackendOptionIncluded(options, report, settings);
        return options;
    }

    private static MemoryWatcherBackendOption AnnotatePreferredMemoryWatcherBackendOption(
        MemoryWatcherBackendOption template,
        MemoryBackendCapabilityReport? report,
        OutbreakTrackerSettings settings
    )
    {
        (CapabilityStatusDisplay status, string stateSummary) = BuildPreferredMemoryWatcherBackendState(
            template,
            report,
            settings
        );
        return template with { Status = status, StateSummary = stateSummary };
    }

    private static (CapabilityStatusDisplay Status, string StateSummary) BuildPreferredMemoryWatcherBackendState(
        MemoryWatcherBackendOption template,
        MemoryBackendCapabilityReport? report,
        OutbreakTrackerSettings settings
    )
    {
        if (report is null)
        {
            return (
                CreateNeedsWorkStatus("Scan needed"),
                "Refresh the Memory scan to classify this backend on the current host and attached target."
            );
        }

        if (template.Value == WatchBackendKind.Auto)
        {
            MemoryWatchNegotiatedCapability? resolved = FindRequestedCapability(
                report.MemoryWatcher.Capabilities,
                settings
            );

            string stateSummary = resolved is null
                ? "Current state: no live capability match is available from the latest scan."
                : $"Current state: resolves to {GetWatchBackendLabel(resolved.Backend)}; {BuildShortConstraintHint(resolved)}";

            return (
                resolved is null ? CreateNeedsWorkStatus("Needs scan match") : GetStatusDisplay(resolved),
                stateSummary
            );
        }

        MemoryWatchNegotiatedCapability? capability = report.MemoryWatcher.Capabilities.FirstOrDefault(candidate =>
            candidate.Backend == template.Value
        );

        if (capability is null)
        {
            return (
                CreateNeedsWorkStatus("No verdict"),
                "Current state: this scan did not return a verdict for this backend."
            );
        }

        return (GetStatusDisplay(capability), $"Current state: {BuildShortConstraintHint(capability)}");
    }

    private static IReadOnlyList<MemoryWatcherPrecisionOption> BuildPreferredMemoryWatcherPrecisionOptions(
        MemoryBackendCapabilityReport? report,
        OutbreakTrackerSettings settings
    )
    {
        List<MemoryWatcherPrecisionOption> options = new(PreferredMemoryWatcherPrecisionOptionValues.Length);
        for (int i = 0; i < PreferredMemoryWatcherPrecisionOptionValues.Length; i++)
        {
            MemoryWatcherPrecisionOption template = PreferredMemoryWatcherPrecisionOptionValues[i];
            if (!ShouldExposePreferredPrecisionOption(template.Value, report, settings))
            {
                continue;
            }

            options.Add(AnnotatePreferredMemoryWatcherPrecisionOption(template, report, settings));
        }

        EnsureCurrentPreferredPrecisionOptionIncluded(options, report, settings);
        return options;
    }

    private static MemoryWatcherPrecisionOption AnnotatePreferredMemoryWatcherPrecisionOption(
        MemoryWatcherPrecisionOption template,
        MemoryBackendCapabilityReport? report,
        OutbreakTrackerSettings settings
    )
    {
        (CapabilityStatusDisplay status, string stateSummary) = BuildPreferredMemoryWatcherPrecisionState(
            template,
            report,
            settings
        );
        return template with { Status = status, StateSummary = stateSummary };
    }

    private static (CapabilityStatusDisplay Status, string StateSummary) BuildPreferredMemoryWatcherPrecisionState(
        MemoryWatcherPrecisionOption template,
        MemoryBackendCapabilityReport? report,
        OutbreakTrackerSettings settings
    )
    {
        if (report is null)
        {
            return (
                CreateNeedsWorkStatus("Scan needed"),
                "Refresh the Memory scan to classify this precision on the current host and attached target."
            );
        }

        if (
            settings.MemoryWatcher.PreferredBackend != WatchBackendKind.Auto
            && TryGetFixedPrecisionForBackend(
                settings.MemoryWatcher.PreferredBackend,
                out WatchPrecision fixedPrecision
            )
            && template.Value != fixedPrecision
        )
        {
            return (
                CreateUnsupportedStatus("Locked by backend"),
                $"Current state: {GetWatchBackendLabel(settings.MemoryWatcher.PreferredBackend)} fixes precision to {GetWatchPrecisionLabel(fixedPrecision)}."
            );
        }

        OutbreakTrackerSettings precisionSettings = settings with
        {
            MemoryWatcher = settings.MemoryWatcher with { PreferredPrecision = template.Value },
        };
        MemoryWatchNegotiatedCapability? resolved = ResolvePrecisionOverrideCapability(
            report.MemoryWatcher.Capabilities,
            precisionSettings
        );

        if (resolved is null)
        {
            return (
                CreateNeedsWorkStatus("No direct match"),
                "Current state: no OT2-safe backend from the latest scan matches this precision override directly."
            );
        }

        return (
            GetStatusDisplay(resolved),
            $"Current state: resolves to {GetWatchBackendLabel(resolved.Backend)}; {BuildShortConstraintHint(resolved)}"
        );
    }

    private static bool ShouldExposePreferredBackendOption(
        WatchBackendKind backend,
        MemoryBackendCapabilityReport? report
    )
    {
        if (backend == WatchBackendKind.Auto)
        {
            return true;
        }

        if (report is null)
        {
            return IsSnapshotFamilyBackend(backend);
        }

        MemoryWatchNegotiatedCapability? capability = report.MemoryWatcher.Capabilities.FirstOrDefault(candidate =>
            candidate.Backend == backend
        );
        return capability is not null && IsSelectableOt2Request(capability);
    }

    private static bool ShouldExposePreferredPrecisionOption(
        WatchPrecision precision,
        MemoryBackendCapabilityReport? report,
        OutbreakTrackerSettings settings
    )
    {
        if (
            settings.MemoryWatcher.PreferredBackend != WatchBackendKind.Auto
            && TryGetFixedPrecisionForBackend(
                settings.MemoryWatcher.PreferredBackend,
                out WatchPrecision fixedPrecision
            )
        )
        {
            return precision == fixedPrecision;
        }

        if (report is null)
        {
            return precision == WatchPrecision.SnapshotBitExact;
        }

        if (precision == WatchPrecision.SoftDirtyThenBitDiff && !report.Host.SupportsSoftDirtyTracking)
        {
            return false;
        }

        OutbreakTrackerSettings precisionSettings = settings with
        {
            MemoryWatcher = settings.MemoryWatcher with { PreferredPrecision = precision },
        };
        MemoryWatchNegotiatedCapability? resolved = ResolvePrecisionOverrideCapability(
            report.MemoryWatcher.Capabilities,
            precisionSettings
        );
        return resolved is not null && IsSelectableOt2Request(resolved);
    }

    private static void EnsureCurrentPreferredBackendOptionIncluded(
        List<MemoryWatcherBackendOption> options,
        MemoryBackendCapabilityReport? report,
        OutbreakTrackerSettings settings
    )
    {
        if (options.Any(option => option.Value == settings.MemoryWatcher.PreferredBackend))
        {
            return;
        }

        MemoryWatcherBackendOption? template = Array.Find(
            PreferredMemoryWatcherBackendOptionValues,
            option => option.Value == settings.MemoryWatcher.PreferredBackend
        );
        if (template is not null)
        {
            options.Add(AnnotatePreferredMemoryWatcherBackendOption(template, report, settings));
        }
    }

    private static void EnsureCurrentPreferredPrecisionOptionIncluded(
        List<MemoryWatcherPrecisionOption> options,
        MemoryBackendCapabilityReport? report,
        OutbreakTrackerSettings settings
    )
    {
        if (options.Any(option => option.Value == settings.MemoryWatcher.PreferredPrecision))
        {
            return;
        }

        MemoryWatcherPrecisionOption? template = Array.Find(
            PreferredMemoryWatcherPrecisionOptionValues,
            option => option.Value == settings.MemoryWatcher.PreferredPrecision
        );
        if (template is not null)
        {
            options.Add(AnnotatePreferredMemoryWatcherPrecisionOption(template, report, settings));
        }
    }

    private static bool IsSelectableOt2Request(MemoryWatchNegotiatedCapability capability)
    {
        CapabilityConstraintDisplay constraint = DescribeCapabilityConstraint(capability);
        return capability.CurrentRequestAvailable
            || constraint.CanApply
            || constraint.Kind == MemoryCapabilityConstraintKind.NeedsAttachedTarget;
    }

    private static bool IsSnapshotFamilyBackend(WatchBackendKind backend) =>
        backend
            is WatchBackendKind.Snapshot
                or WatchBackendKind.HashIndexedSnapshot
                or WatchBackendKind.SegmentedSnapshot;

    private static MemoryWatchNegotiatedCapability? ResolvePrecisionOverrideCapability(
        IReadOnlyList<MemoryWatchNegotiatedCapability> capabilities,
        OutbreakTrackerSettings settings
    )
    {
        if (capabilities.Count == 0)
        {
            return null;
        }

        if (settings.MemoryWatcher.PreferredBackend != WatchBackendKind.Auto)
        {
            return FindRequestedCapability(capabilities, settings);
        }

        return capabilities
            .Where(capability => GetEffectivePrecision(capability) == settings.MemoryWatcher.PreferredPrecision)
            .OrderByDescending(static capability => capability.CurrentRequestAvailable)
            .ThenByDescending(static capability =>
                capability.EnvironmentSupport == MemoryCapabilitySupportLevel.Supported
            )
            .ThenBy(static capability => GetInvasivenessRank(capability.Invasiveness))
            .ThenByDescending(static capability => GetLatencyRank(capability.LatencyClass))
            .FirstOrDefault();
    }

    private static string BuildShortConstraintHint(MemoryWatchNegotiatedCapability capability)
    {
        if (capability.CurrentRequestAvailable)
        {
            return "ready now on this target.";
        }

        CapabilityConstraintDisplay constraint = DescribeCapabilityConstraint(capability);
        return constraint.Kind switch
        {
            MemoryCapabilityConstraintKind.NeedsIntrusiveOptIn => "enable intrusive backends and OT2 can request it.",
            MemoryCapabilityConstraintKind.NeedsProbeValidation =>
                "the host path exists, but the last live probe did not arm cleanly.",
            MemoryCapabilityConstraintKind.NeedsOt2Helper =>
                "it needs a cooperative helper or in-process producer that OT2 does not run today.",
            MemoryCapabilityConstraintKind.NeedsThreadScopedIntegration
            or MemoryCapabilityConstraintKind.NotSupportedByOt2Model =>
                "it needs a finer OT2 watch model than the current grouped-reader shape.",
            MemoryCapabilityConstraintKind.NeedsOt2RuntimeIntegration =>
                "this OT2 deployment does not expose the runtime path it expects.",
            MemoryCapabilityConstraintKind.NotSupportedByHost when IsPackagedRuntimeSurfaceUnavailable(capability) =>
                "the packaged runtime on this host does not surface it.",
            MemoryCapabilityConstraintKind.NotSupportedByHost =>
                "it is unavailable on this host or with the current rights.",
            MemoryCapabilityConstraintKind.NeedsAttachedTarget => "attach a live PCSX2 target to verify it.",
            _ => "it still needs additional setup on this target.",
        };
    }

    private void ApplyMemoryWatcherCapabilitySections(IReadOnlyList<MemoryWatcherCapabilityCard> cards)
    {
        List<MemoryWatcherCapabilityCard> ready = [];
        List<MemoryWatcherCapabilityCard> actionable = [];
        List<MemoryWatcherCapabilityCard> future = [];
        List<MemoryWatcherCapabilityCard> unavailable = [];

        for (int i = 0; i < cards.Count; i++)
        {
            MemoryWatcherCapabilityCard card = cards[i];
            switch (ClassifyCardBucket(card))
            {
                case MemoryCapabilityBucket.Ready:
                    ready.Add(card);
                    break;
                case MemoryCapabilityBucket.Actionable:
                    actionable.Add(card);
                    break;
                case MemoryCapabilityBucket.Future:
                    future.Add(card);
                    break;
                default:
                    unavailable.Add(card);
                    break;
            }
        }

        ReadyMemoryWatcherCapabilityCards = OrderMemoryWatcherCapabilityCards(ready);
        ActionableMemoryWatcherCapabilityCards = OrderMemoryWatcherCapabilityCards(actionable);
        FutureMemoryWatcherCapabilityCards = OrderMemoryWatcherCapabilityCards(future);
        UnavailableMemoryWatcherCapabilityCards = OrderMemoryWatcherCapabilityCards(unavailable);
        ReadyMemoryWatcherCapabilitySummary = ReadyMemoryWatcherCapabilityCards.Count switch
        {
            0 => "No strategy is ready on the attached target yet.",
            1 => "One strategy is ready right now on the attached target.",
            _ => $"{ReadyMemoryWatcherCapabilityCards.Count} strategies are ready right now on the attached target.",
        };
        ActionableMemoryWatcherCapabilitySummary = ActionableMemoryWatcherCapabilityCards.Count switch
        {
            0 => "No attach-now or one-step OT2 actions are pending.",
            1 => "One strategy can be enabled now with one explicit OT2 step or a live target validation pass.",
            _ =>
                $"{ActionableMemoryWatcherCapabilityCards.Count} strategies can be enabled now with one explicit OT2 step or a live target validation pass.",
        };
        FutureMemoryWatcherCapabilitySummary = FutureMemoryWatcherCapabilityCards.Count switch
        {
            0 => "No deeper OT2/helper work items are currently surfaced by the scan.",
            1 =>
                "One strategy still needs a helper path, finer watch shape, or deeper OT2/runtime wiring before it becomes selectable.",
            _ =>
                $"{FutureMemoryWatcherCapabilityCards.Count} strategies still need a helper path, finer watch shape, or deeper OT2/runtime wiring before they become selectable.",
        };
        UnavailableMemoryWatcherCapabilitySummary = UnavailableMemoryWatcherCapabilityCards.Count switch
        {
            0 => "No strategy is fully blocked by the current host/runtime.",
            1 => "One strategy is blocked by the current host, runtime, packaged backend, or rights.",
            _ =>
                $"{UnavailableMemoryWatcherCapabilityCards.Count} strategies are blocked by the current host, runtime, packaged backend, or rights.",
        };
    }

    private static string BuildHostSummary(MemoryWatchHostEnvironment host)
    {
        return JoinLines(
            $"OS: {host.OperatingSystem}",
            $"Architecture: {host.ProcessArchitecture}",
            $"Runtime: {host.RuntimeDescription}",
            $"Privileges: {(host.IsElevatedUser ? "elevated" : "standard user")}",
            $"Packaged native runtime: {(host.SupportsPackagedRemoteAot ? "available" : "unavailable")}"
        );
    }

    private static string BuildTargetSummary(MemoryWatchTargetEnvironment target)
    {
        if (!target.ProcessFound)
        {
            return JoinLines(
                "Target: no running PCSX2 process is attached.",
                $"Session: {target.SessionFailureReason ?? "unavailable until OT2 is attached"}"
            );
        }

        string processLabel = string.IsNullOrWhiteSpace(target.ProcessName)
            ? $"PID {target.ProcessId}"
            : $"{target.ProcessName} (PID {target.ProcessId})";
        return target.SessionOpened
            ? JoinLines($"Target: {processLabel}", "Session: opened successfully")
            : JoinLines(
                $"Target: {processLabel}",
                "Session: failed to open",
                $"Reason: {target.SessionFailureReason ?? "unknown"}"
            );
    }

    private static string BuildNativeLibrarySummary(MemoryWatcherSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.NativeLibraryPath))
        {
            return "Native runtime: packaged MemoryWatcher runtime.";
        }

        string path = settings.NativeLibraryPath;
        return File.Exists(path)
            ? $"Native runtime: override file at {path}"
            : $"Native runtime: override path is configured but missing at {path}";
    }

    private static IReadOnlyList<MemoryBackendCard> BuildBackendCapabilityCards(
        MemoryBackendCapabilityReport report,
        OutbreakTrackerSettings settings
    )
    {
        MemoryBackendMode recommendedMode = SelectRecommendedBackendMode(report);
        MemoryWatchNegotiatedCapability? requestedStrategy = FindRequestedCapability(
            report.MemoryWatcher.Capabilities,
            settings
        );

        MemoryBackendCard[] cards = new MemoryBackendCard[report.Backends.Count];
        for (int i = 0; i < report.Backends.Count; i++)
        {
            MemoryBackendCapability backend = report.Backends[i];
            CapabilityStatusDisplay status =
                backend.Mode == MemoryBackendMode.MemoryWatcher && requestedStrategy is not null
                    ? GetStatusDisplay(requestedStrategy)
                    : GetStatusDisplay(backend.Support == MemoryCapabilitySupportLevel.Supported, backend.Support);
            cards[i] = new MemoryBackendCard(
                backend.Mode,
                GetMemoryBackendModeLabel(backend.Mode),
                backend.Mode == MemoryBackendMode.Legacy
                    ? "Compatibility reader with timer-driven refreshes."
                    : "Grouped reads with capability negotiation and future wait-capable paths.",
                status,
                BuildCapabilityMetrics(backend.Invasiveness, backend.PrecisionClass, backend.LatencyClass),
                BuildBackendStatusSummary(backend, requestedStrategy),
                BuildBackendReason(backend, requestedStrategy),
                BuildBackendPickWhen(backend.Mode),
                settings.MemoryWatcher.Backend == backend.Mode,
                backend.Mode == recommendedMode
            );
        }

        return cards;
    }

    private static IReadOnlyList<MemoryWatcherCapabilityCard> BuildMemoryWatcherCapabilityCards(
        MemoryBackendCapabilityReport report,
        OutbreakTrackerSettings settings
    )
    {
        MemoryWatchNegotiatedCapability? requested = FindRequestedCapability(
            report.MemoryWatcher.Capabilities,
            settings
        );
        MemoryWatchNegotiatedCapability? recommended =
            report.MemoryWatcher.Capabilities.Count == 0
                ? null
                : SelectBestCapability(report.MemoryWatcher.Capabilities);
        MemoryWatcherCapabilityCard[] cards = new MemoryWatcherCapabilityCard[report.MemoryWatcher.Capabilities.Count];
        for (int i = 0; i < report.MemoryWatcher.Capabilities.Count; i++)
        {
            MemoryWatchNegotiatedCapability capability = report.MemoryWatcher.Capabilities[i];
            WatchPrecision precision = GetEffectivePrecision(capability);
            bool isSelected = requested is not null && requested.Backend == capability.Backend;
            bool isRecommended =
                recommended is not null
                && recommended.Backend == capability.Backend
                && recommended.CurrentRequestAvailable;
            CapabilityConstraintDisplay constraint = DescribeCapabilityConstraint(capability);
            CapabilityStatusDisplay status = GetStatusDisplay(capability);

            cards[i] = new MemoryWatcherCapabilityCard(
                capability.Backend,
                precision,
                GetWatchBackendLabel(capability.Backend),
                GetCapabilityTagline(capability.Backend),
                GetHostStatusDisplay(capability),
                status,
                constraint.Kind,
                BuildHostAvailabilitySummary(capability),
                constraint.Label,
                BuildCapabilityStatusSummary(capability, constraint),
                BuildCapabilityMetrics(capability.Invasiveness, capability.PrecisionClass, capability.LatencyClass),
                BuildCapabilityReason(capability, constraint),
                BuildCapabilityPickWhen(capability, isRecommended),
                constraint.ButtonText,
                constraint.CanApply,
                isSelected,
                isRecommended
            );
        }

        return OrderMemoryWatcherCapabilityCards(cards);
    }

    private static IReadOnlyList<MemoryWatcherCapabilityCard> OrderMemoryWatcherCapabilityCards(
        IEnumerable<MemoryWatcherCapabilityCard> cards
    ) =>
        cards
            .OrderBy(static card => GetBucketSortRank(ClassifyCardBucket(card)))
            .ThenByDescending(static card => card.IsSelected)
            .ThenByDescending(static card => card.IsRecommended)
            .ThenBy(static card => GetBackendSortRank(card.Backend))
            .ThenBy(static card => card.Title, StringComparer.Ordinal)
            .ToArray();

    private static MemoryCapabilityBucket ClassifyCardBucket(MemoryWatcherCapabilityCard card)
    {
        if (card.Status.Kind == MemoryCapabilityDisplayKind.Ready)
        {
            return MemoryCapabilityBucket.Ready;
        }

        if (card.Status.Kind == MemoryCapabilityDisplayKind.Unsupported)
        {
            return MemoryCapabilityBucket.Unavailable;
        }

        return card.CanApply || card.ConstraintKind == MemoryCapabilityConstraintKind.NeedsAttachedTarget
            ? MemoryCapabilityBucket.Actionable
            : MemoryCapabilityBucket.Future;
    }

    private static int GetBucketSortRank(MemoryCapabilityBucket bucket) =>
        bucket switch
        {
            MemoryCapabilityBucket.Ready => 0,
            MemoryCapabilityBucket.Actionable => 1,
            MemoryCapabilityBucket.Future => 2,
            _ => 3,
        };

    private static int GetBackendSortRank(WatchBackendKind backend) =>
        backend switch
        {
            WatchBackendKind.Snapshot => 0,
            WatchBackendKind.HashIndexedSnapshot => 1,
            WatchBackendKind.SegmentedSnapshot => 2,
            WatchBackendKind.HardwareWatchpoint => 3,
            WatchBackendKind.PageFault => 4,
            WatchBackendKind.DirtyPage => 5,
            WatchBackendKind.DirtyRange => 6,
            WatchBackendKind.NativeAgent => 7,
            WatchBackendKind.SoftDirty => 8,
            _ => 9,
        };

    private static string BuildMemoryWatcherPolicySummary(
        MemoryBackendCapabilityReport report,
        OutbreakTrackerSettings settings
    )
    {
        MemoryWatcherSettings memoryWatcher = settings.MemoryWatcher;
        string request = JoinLines(
            $"Requested backend: {GetWatchBackendLabel(memoryWatcher.PreferredBackend)}",
            $"Requested precision: {GetWatchPrecisionLabel(memoryWatcher.PreferredPrecision)}",
            $"Fallback: {(memoryWatcher.AllowFallback ? "enabled" : "disabled")}"
        );

        MemoryWatchNegotiatedCapability? requested = FindRequestedCapability(
            report.MemoryWatcher.Capabilities,
            settings
        );
        if (requested is null)
        {
            return JoinLines(request, "Status: no matching strategy could be resolved from the current scan.");
        }

        CapabilityConstraintDisplay constraint = DescribeCapabilityConstraint(requested);
        CapabilityStatusDisplay availability = GetStatusDisplay(requested);
        return JoinLines(
            request,
            $"Current match: {GetWatchBackendLabel(requested.Backend)}",
            $"Host capability: {GetHostStatusDisplay(requested).Label}",
            $"OT2 today: {availability.Label}",
            MaybePrefix("OT2 blocker: ", constraint.Label),
            BuildCapabilityMetrics(requested.Invasiveness, requested.PrecisionClass, requested.LatencyClass),
            BuildHostAvailabilitySummary(requested),
            BuildCapabilityStatusSummary(requested, constraint),
            MaybePrefix("Why OT2 is not there yet: ", BuildCapabilityReason(requested, constraint))
        );
    }

    private static string BuildRecommendedConfigurationSummary(MemoryBackendCapabilityReport report)
    {
        MemoryBackendMode recommendedBackend = SelectRecommendedBackendMode(report);
        MemoryWatchNegotiatedCapability? recommendedStrategy = SelectBestReadyCapability(
            report.MemoryWatcher.Capabilities
        );
        MemoryWatchNegotiatedCapability? configurableStrategy = SelectBestConfigurableCapability(
            report.MemoryWatcher.Capabilities
        );

        if (!report.MemoryWatcher.Target.ProcessFound)
        {
            if (recommendedBackend == MemoryBackendMode.MemoryWatcher && configurableStrategy is not null)
            {
                List<string> detachedLines =
                [
                    "Best pick right now: MemoryWatcher",
                    "Why: OT2 can safely keep the richer grouped reader configured now, even before a live PCSX2 target is attached.",
                    $"Safe deferred baseline: {GetWatchBackendLabel(configurableStrategy.Backend)}.",
                    "Recommended action: keep OT2 backend on MemoryWatcher and leave Preferred backend on Automatic until PCSX2 is attached, then validate any lower-latency path you want to force.",
                ];

                MemoryWatchNegotiatedCapability? higherPrecisionDeferred = SelectHigherPrecisionSelectableCapability(
                    report.MemoryWatcher.Capabilities
                );
                if (
                    higherPrecisionDeferred is not null
                    && higherPrecisionDeferred.Backend != configurableStrategy.Backend
                    && GetPrecisionRank(higherPrecisionDeferred.PrecisionClass)
                        > GetPrecisionRank(configurableStrategy.PrecisionClass)
                )
                {
                    detachedLines.Add(
                        $"Lower-latency path to validate after attach: {GetWatchBackendLabel(higherPrecisionDeferred.Backend)}. {BuildHigherPrecisionTradeoff(higherPrecisionDeferred)}"
                    );
                }

                return JoinLines([.. detachedLines]);
            }

            return JoinLines(
                "Best pick right now: Legacy",
                "Why: OT2 is not attached to a live PCSX2 process, so the grouped MemoryWatcher request cannot be validated yet.",
                "Next step: attach PCSX2 and rescan if you want a live capability verdict."
            );
        }

        if (
            recommendedBackend == MemoryBackendMode.Legacy
            || (recommendedStrategy is null && configurableStrategy is null)
        )
        {
            return JoinLines(
                "Best pick right now: Legacy",
                "Why: MemoryWatcher does not currently expose a stronger ready-to-use grouped-region path for OT2 on this host and target.",
                "Use this when you want the simplest working reader and you accept timer-driven updates."
            );
        }

        if (recommendedStrategy is null && configurableStrategy is not null)
        {
            return JoinLines(
                "Best pick right now: MemoryWatcher",
                $"Closest OT2-safe path today: {GetWatchBackendLabel(configurableStrategy.Backend)}",
                "Why: the richer OT2 path is valid for this host, but it still needs the target to finish booting or be revalidated live before OT2 can treat it as ready now.",
                "Recommended action: keep OT2 backend on MemoryWatcher, then rescan once PCSX2 is fully attached."
            );
        }

        if (recommendedStrategy is null)
        {
            return JoinLines(
                "Best pick right now: MemoryWatcher",
                "Why: the current scan did not resolve a live ready strategy, but OT2 can still keep the generic MemoryWatcher path selected while you rescan against a real target.",
                "Recommended action: keep OT2 backend on MemoryWatcher, then refresh the scan once PCSX2 is attached and stable."
            );
        }

        MemoryWatchNegotiatedCapability readyStrategy = recommendedStrategy;
        List<string> lines =
        [
            "Best pick right now: MemoryWatcher",
            $"Resolved strategy today: {GetWatchBackendLabel(readyStrategy.Backend)}",
            $"Why this wins: {BuildRecommendedStrategyReason(readyStrategy)}",
            "Recommended action: keep OT2 backend on MemoryWatcher and leave Preferred backend on Automatic unless you intentionally want to force a different path.",
        ];

        MemoryWatchNegotiatedCapability? higherPrecision = SelectHigherPrecisionCapability(
            report.MemoryWatcher.Capabilities
        );
        if (
            higherPrecision is not null
            && higherPrecision.Backend != readyStrategy.Backend
            && GetPrecisionRank(higherPrecision.PrecisionClass) > GetPrecisionRank(readyStrategy.PrecisionClass)
        )
        {
            lines.Add(
                $"Higher-precision option: {GetWatchBackendLabel(higherPrecision.Backend)}. {BuildHigherPrecisionTradeoff(higherPrecision)}"
            );
        }

        return JoinLines([.. lines]);
    }

    private static string BuildRequestPolicyScopeSummary(
        MemoryBackendCapabilityReport report,
        OutbreakTrackerSettings settings
    )
    {
        List<string> selectableBackends = [];
        foreach (MemoryWatchNegotiatedCapability capability in report.MemoryWatcher.Capabilities)
        {
            if (!IsSelectableOt2Request(capability))
            {
                continue;
            }

            selectableBackends.Add(GetWatchBackendLabel(capability.Backend));
        }

        List<string> selectablePrecisions = [];
        foreach (
            WatchPrecision precision in PreferredMemoryWatcherPrecisionOptionValues.Select(static option =>
                option.Value
            )
        )
        {
            if (!ShouldExposePreferredPrecisionOption(precision, report, settings))
            {
                continue;
            }

            selectablePrecisions.Add(GetWatchPrecisionLabel(precision));
        }

        string backendSummary =
            selectableBackends.Count == 0
                ? "Automatic is the only safe request choice from the latest scan."
                : $"Safe request choices from this scan: Automatic, {string.Join(", ", selectableBackends)}.";

        string precisionSummary =
            selectablePrecisions.Count == 0
                ? "No explicit precision override is safe from the latest scan yet."
                : $"Safe precision overrides from this scan: {string.Join(", ", selectablePrecisions)}.";

        return JoinLines(
            backendSummary,
            precisionSummary,
            "Automatic intentionally favors OT2's safest compatible grouped-read default. It does not automatically jump to the sharpest or most invasive wake path just because that path exists on the host.",
            "In OT2 today, Page Fault, Dirty Page, and Hardware Watchpoint requests still feed grouped snapshot-backed decoding. They change how OT2 wakes or invalidates those grouped reads rather than swapping to a completely different decode model.",
            "Helper-only, Linux-only, runtime-missing, and deeper-integration strategies stay visible below as strategy cards, but OT2 intentionally keeps them out of this request picker."
        );
    }

    private static string BuildSupportLegendSummary() =>
        JoinLines(
            "Green - Ready now: OT2 can create and use this strategy immediately on the attached PCSX2 target.",
            "Blue - Action needed: the host path exists, but OT2 still needs something specific such as a live target, opt-in, helper path, successful live probe, or deeper watch-model integration.",
            "Red - Unavailable here: the host, runtime, packaged backend, or current rights cannot expose this strategy here."
        );

    private static string BuildMemoryCadenceSummary(
        MemoryBackendCapabilityReport report,
        OutbreakTrackerSettings settings
    )
    {
        int fastMs = settings.DataManager.FastUpdateIntervalMs;
        int slowMs = settings.DataManager.SlowUpdateIntervalMs;

        if (settings.MemoryWatcher.Backend == MemoryBackendMode.Legacy)
        {
            return JoinLines(
                "Current model: fully timer-driven.",
                $"In-game OT2 refreshes are paced by the fast interval ({fastMs} ms), while lobby and lower-priority refreshes use the slow interval ({slowMs} ms).",
                "No MemoryWatcher wake path is active in this mode."
            );
        }

        MemoryWatchNegotiatedCapability? requested = FindRequestedCapability(
            report.MemoryWatcher.Capabilities,
            settings
        );
        if (requested is null)
        {
            return JoinLines(
                "Current model: MemoryWatcher requested, but the latest scan did not resolve a matching strategy.",
                $"Until OT2 can resolve that request, the fast interval ({fastMs} ms) and slow interval ({slowMs} ms) are the only dependable cadence guardrails."
            );
        }

        CapabilityConstraintDisplay constraint = DescribeCapabilityConstraint(requested);
        string cadenceGuardrail =
            $"Fast interval: {fastMs} ms in gameplay. Slow interval: {slowMs} ms in lobby or other lower-priority refresh paths.";

        if (requested.CurrentRequestAvailable)
        {
            if (
                requested.Backend
                is WatchBackendKind.PageFault
                    or WatchBackendKind.DirtyPage
                    or WatchBackendKind.HardwareWatchpoint
            )
            {
                return JoinLines(
                    "Current model: event wakeups over grouped snapshot decoding.",
                    $"OT2 will keep its grouped snapshot-backed reads, but this backend can wake or invalidate those grouped regions earlier than the timer cadence when activity is detected.",
                    cadenceGuardrail,
                    "Those intervals now act as OT2's max-idle guardrails rather than the only way updates can happen."
                );
            }

            if (requested.CurrentCapability?.EventDriven == true)
            {
                return JoinLines(
                    "Current model: event-capable MemoryWatcher strategy.",
                    "OT2 still decodes from grouped snapshots, but the active MemoryWatcher strategy can signal those grouped refreshes earlier than plain timer cadence.",
                    cadenceGuardrail
                );
            }

            return JoinLines(
                "Current model: grouped snapshot comparison remains the main cadence driver.",
                "OT2 is using MemoryWatcher for grouped snapshots, but this request still behaves like a sampled snapshot path rather than a dedicated wake backend.",
                cadenceGuardrail
            );
        }

        return constraint.Kind switch
        {
            MemoryCapabilityConstraintKind.NeedsAttachedTarget => JoinLines(
                "Current model: deferred MemoryWatcher request awaiting live validation.",
                "OT2 can keep this request configured now, but it cannot prove the wake path until a real PCSX2 target is attached and rescanned.",
                cadenceGuardrail
            ),
            MemoryCapabilityConstraintKind.NeedsIntrusiveOptIn => JoinLines(
                "Current model: opt-in gated wake path.",
                "The selected backend is designed to wake grouped snapshot refreshes earlier, but OT2 will not request it until intrusive backends are explicitly allowed.",
                cadenceGuardrail
            ),
            _ => JoinLines(
                "Current model: fallback cadence still matters.",
                "The latest scan did not show this request as ready now, so OT2 should be treated as timer-guarded until the missing probe, helper, runtime, or target condition is resolved.",
                cadenceGuardrail
            ),
        };
    }

    private static string BuildCapabilityRealitySummary(MemoryWatchCapabilityNegotiationResult report)
    {
        List<string> ready = [];
        List<string> optIn = [];
        List<string> helper = [];
        List<string> probe = [];
        List<string> finerWatch = [];
        List<string> wiring = [];
        List<string> runtimeMissing = [];
        List<string> hostUnavailable = [];
        List<string> targetNeeded = [];
        List<string> realisticExternalUpgrades = [];

        foreach (MemoryWatchNegotiatedCapability capability in report.Capabilities)
        {
            CapabilityConstraintDisplay constraint = DescribeCapabilityConstraint(capability);
            string label = GetWatchBackendLabel(capability.Backend);

            if (capability.CurrentRequestAvailable)
            {
                ready.Add(label);
                continue;
            }

            if (
                capability.Backend is WatchBackendKind.PageFault or WatchBackendKind.HardwareWatchpoint
                && capability.EnvironmentSupport == MemoryCapabilitySupportLevel.Supported
            )
            {
                realisticExternalUpgrades.Add(label);
            }

            switch (constraint.Kind)
            {
                case MemoryCapabilityConstraintKind.NeedsIntrusiveOptIn:
                    optIn.Add(label);
                    break;
                case MemoryCapabilityConstraintKind.NeedsOt2Helper:
                    helper.Add(label);
                    break;
                case MemoryCapabilityConstraintKind.NeedsProbeValidation:
                    probe.Add(label);
                    break;
                case MemoryCapabilityConstraintKind.NeedsThreadScopedIntegration:
                case MemoryCapabilityConstraintKind.NotSupportedByOt2Model:
                    finerWatch.Add(label);
                    break;
                case MemoryCapabilityConstraintKind.NeedsOt2RuntimeIntegration:
                    wiring.Add(label);
                    break;
                case MemoryCapabilityConstraintKind.NeedsAttachedTarget:
                    targetNeeded.Add(label);
                    break;
                case MemoryCapabilityConstraintKind.NotSupportedByHost
                    when IsPackagedRuntimeSurfaceUnavailable(capability):
                    runtimeMissing.Add(label);
                    break;
                case MemoryCapabilityConstraintKind.NotSupportedByHost:
                    hostUnavailable.Add(label);
                    break;
            }
        }

        List<string> deployment = [];
        if (report.Target.ProcessFound)
        {
            deployment.Add(
                "Current OT2 deployment class: external grouped PCSX2 reader with no cooperative in-process helper."
            );
        }

        if (ready.Count > 0)
        {
            deployment.Add(
                "That makes the snapshot family the safe baseline, with event-driven external signal paths as the only realistic low-latency upgrade class on this host."
            );
        }

        if (realisticExternalUpgrades.Count > 0)
        {
            deployment.Add(
                $"Realistic lower-latency external upgrades here: {string.Join(", ", realisticExternalUpgrades)}."
            );
        }

        if (helper.Count > 0)
        {
            deployment.Add(
                "Helper-class strategies stay blue because they need a cooperative producer or in-process helper, not because the generic idea is invalid."
            );
        }

        if (wiring.Count > 0 || finerWatch.Count > 0)
        {
            deployment.Add(
                "Some strategies are blocked by OT2's current grouped-watch shape rather than by Windows or MemoryWatcher itself. Those need deeper OT2 integration, not just a toggle."
            );
        }

        return JoinLines(
            deployment.Count > 0 ? string.Join(Environment.NewLine, deployment) : null,
            ready.Count > 0 ? $"Ready now: {string.Join(", ", ready)}" : null,
            optIn.Count > 0 ? $"One setting away: {string.Join(", ", optIn)}" : null,
            probe.Count > 0 ? $"Needs live probe retry: {string.Join(", ", probe)}" : null,
            helper.Count > 0 ? $"Needs helper path: {string.Join(", ", helper)}" : null,
            finerWatch.Count > 0 ? $"Needs finer OT2 watch model: {string.Join(", ", finerWatch)}" : null,
            wiring.Count > 0 ? $"Needs OT2/runtime wiring: {string.Join(", ", wiring)}" : null,
            runtimeMissing.Count > 0
                ? $"Missing from current packaged runtime: {string.Join(", ", runtimeMissing)}"
                : null,
            hostUnavailable.Count > 0 ? $"Not available on this host: {string.Join(", ", hostUnavailable)}" : null,
            targetNeeded.Count > 0 ? $"Needs live PCSX2 target to verify: {string.Join(", ", targetNeeded)}" : null
        );
    }

    private static bool IsPackagedRuntimeSurfaceUnavailable(MemoryWatchNegotiatedCapability capability)
    {
        if (capability.CurrentRequestAvailable || capability.CurrentCapability is not null)
        {
            return false;
        }

        if (capability.CurrentRequestConstraintKind != RemoteConstraintKind.BackendNotSurfacedForRegion)
        {
            return false;
        }

        return capability.Backend == WatchBackendKind.DirtyPage;
    }

    private static bool IsLiveProbeFailure(string? reason) =>
        !string.IsNullOrWhiteSpace(reason)
        && reason.StartsWith("Live probe creation failed:", StringComparison.Ordinal);

    private static string BuildCapabilityMetrics(
        MemoryObservationInvasiveness invasiveness,
        MemoryObservationPrecisionClass precision,
        MemoryObservationLatencyClass latency
    ) =>
        JoinLines(
            $"Invasiveness: {FormatInvasiveness(invasiveness)}",
            $"Precision: {FormatPrecision(precision)}",
            $"Latency: {FormatLatency(latency)}"
        );

    private static MemoryBackendMode SelectRecommendedBackendMode(MemoryBackendCapabilityReport report)
    {
        bool hasUsableMemoryWatcherPath = report.MemoryWatcher.Capabilities.Any(static capability =>
            capability.CurrentRequestAvailable
        );

        if (hasUsableMemoryWatcherPath)
        {
            return MemoryBackendMode.MemoryWatcher;
        }

        bool hasConfigurableMemoryWatcherPath = report.MemoryWatcher.Capabilities.Any(IsSelectableOt2Request);
        if (hasConfigurableMemoryWatcherPath)
        {
            return MemoryBackendMode.MemoryWatcher;
        }

        MemoryBackendCapability? legacy = report.Backends.FirstOrDefault(static backend =>
            backend.Mode == MemoryBackendMode.Legacy
        );
        return legacy?.Support != MemoryCapabilitySupportLevel.Unsupported
            ? MemoryBackendMode.Legacy
            : MemoryBackendMode.MemoryWatcher;
    }

    private static CapabilityStatusDisplay GetStatusDisplay(
        bool currentRequestAvailable,
        MemoryCapabilitySupportLevel support
    )
    {
        if (currentRequestAvailable)
        {
            return CreateReadyStatus("Ready now");
        }

        return support switch
        {
            MemoryCapabilitySupportLevel.Unsupported => CreateUnsupportedStatus("Unavailable here"),
            _ => CreateNeedsWorkStatus("Ready after attach"),
        };
    }

    private static CapabilityStatusDisplay GetStatusDisplay(MemoryWatchNegotiatedCapability capability)
    {
        CapabilityConstraintDisplay constraint = DescribeCapabilityConstraint(capability);
        if (capability.CurrentRequestAvailable)
        {
            return CreateReadyStatus("Ready now");
        }

        if (IsPackagedRuntimeSurfaceUnavailable(capability))
        {
            return CreateUnsupportedStatus("Unavailable here");
        }

        return constraint.Kind switch
        {
            MemoryCapabilityConstraintKind.NotSupportedByHost => CreateUnsupportedStatus("Unavailable here"),
            MemoryCapabilityConstraintKind.NeedsAttachedTarget => CreateNeedsWorkStatus("Ready after attach"),
            MemoryCapabilityConstraintKind.NeedsIntrusiveOptIn => CreateNeedsWorkStatus("Ready after opt-in"),
            MemoryCapabilityConstraintKind.NeedsOt2Helper => CreateNeedsWorkStatus("Helper path needed"),
            MemoryCapabilityConstraintKind.NeedsProbeValidation => CreateNeedsWorkStatus("Retry live probe"),
            MemoryCapabilityConstraintKind.NeedsOt2RuntimeIntegration => CreateNeedsWorkStatus("OT2/runtime gap"),
            MemoryCapabilityConstraintKind.NeedsThreadScopedIntegration
            or MemoryCapabilityConstraintKind.NotSupportedByOt2Model => CreateNeedsWorkStatus("Point-watch gap"),
            _ => CreateNeedsWorkStatus("Action needed"),
        };
    }

    private static CapabilityStatusDisplay GetHostStatusDisplay(MemoryWatchNegotiatedCapability capability)
    {
        if (IsPackagedRuntimeSurfaceUnavailable(capability))
        {
            return CreateUnsupportedStatus("Runtime missing");
        }

        if (IsLiveProbeFailure(capability.EnvironmentSupportReason))
        {
            return CreateNeedsWorkStatus("Probe failed");
        }

        if (capability.EnvironmentSupport == MemoryCapabilitySupportLevel.Supported)
        {
            return CreateReadyStatus("Host supports it");
        }

        if (capability.EnvironmentSupport == MemoryCapabilitySupportLevel.Unsupported)
        {
            return CreateUnsupportedStatus("Host blocked");
        }

        return capability.EnvironmentConstraintKind switch
        {
            RemoteConstraintKind.RequiresAgent or RemoteConstraintKind.RequiresCooperativeProducer =>
                CreateNeedsWorkStatus("Host needs helper"),
            RemoteConstraintKind.RequiresPageTracker => CreateNeedsWorkStatus("Host needs tracker"),
            RemoteConstraintKind.TargetProcessMissing
            or RemoteConstraintKind.SessionUnavailable
            or RemoteConstraintKind.MissingRegion => CreateNeedsWorkStatus("Attach target"),
            _ => CreateNeedsWorkStatus("Host conditional"),
        };
    }

    private static CapabilityStatusDisplay CreateReadyStatus(string label) =>
        new(
            MemoryCapabilityDisplayKind.Ready,
            label,
            ReadyBadgeBackgroundBrush,
            ReadyBadgeBorderBrush,
            ReadyBadgeForegroundBrush
        );

    private static CapabilityStatusDisplay CreateNeedsWorkStatus(string label) =>
        new(
            MemoryCapabilityDisplayKind.NeedsWork,
            label,
            NeedsWorkBadgeBackgroundBrush,
            NeedsWorkBadgeBorderBrush,
            NeedsWorkBadgeForegroundBrush
        );

    private static CapabilityStatusDisplay CreateUnsupportedStatus(string label) =>
        new(
            MemoryCapabilityDisplayKind.Unsupported,
            label,
            UnsupportedBadgeBackgroundBrush,
            UnsupportedBadgeBorderBrush,
            UnsupportedBadgeForegroundBrush
        );

    private static string BuildBackendStatusSummary(
        MemoryBackendCapability backend,
        MemoryWatchNegotiatedCapability? requestedStrategy
    )
    {
        return backend.Mode switch
        {
            MemoryBackendMode.Legacy => backend.Support == MemoryCapabilitySupportLevel.Supported
                ? "Ready now: OT2 can read the attached process immediately, but all refreshes remain timer-driven."
                : "This stays purely external and timer-driven, and OT2 still needs a live PCSX2 process attached before it can verify the path.",
            MemoryBackendMode.MemoryWatcher when requestedStrategy is not null =>
                requestedStrategy.CurrentRequestAvailable
                    ? requestedStrategy.Backend switch
                    {
                        WatchBackendKind.HardwareWatchpoint =>
                            "Ready now: OT2 will arm a tiny hardware wake sentinel inside EEmem and refresh its grouped snapshots when that sentinel trips.",
                        WatchBackendKind.PageFault =>
                            "Ready now: OT2 will arm page-backed wake watches and refresh the affected grouped snapshot domains when those pages trip.",
                        WatchBackendKind.DirtyPage =>
                            "Ready now: OT2 will consume page-dirty invalidation signals and refresh the affected grouped snapshot domains.",
                        _ =>
                            $"Ready now: OT2 will open MemoryWatcher through {GetWatchBackendLabel(requestedStrategy.Backend)} on its grouped EEmem watch.",
                    }
                    : $"Not ready today: {DescribeCapabilityConstraint(requestedStrategy).Label}.",
            MemoryBackendMode.MemoryWatcher =>
                "MemoryWatcher is the richer OT2 integration path when the host exposes a usable strategy.",
            _ => string.Empty,
        };
    }

    private static string? BuildBackendReason(
        MemoryBackendCapability backend,
        MemoryWatchNegotiatedCapability? requestedStrategy
    )
    {
        if (backend.Mode == MemoryBackendMode.MemoryWatcher && requestedStrategy is not null)
        {
            return BuildCapabilityReason(requestedStrategy, DescribeCapabilityConstraint(requestedStrategy));
        }

        return backend.Reason;
    }

    private static string BuildBackendPickWhen(MemoryBackendMode mode) =>
        mode switch
        {
            MemoryBackendMode.Legacy =>
                "Best for: simplest compatibility path when you do not need MemoryWatcher negotiation.",
            MemoryBackendMode.MemoryWatcher =>
                "Best for: grouped reads now, plus cleaner upgrade room for lower-latency strategies later.",
            _ => string.Empty,
        };

    private static string GetCapabilityTagline(WatchBackendKind backend) =>
        backend switch
        {
            WatchBackendKind.Snapshot => "Safest generic default with grouped snapshot comparison.",
            WatchBackendKind.HashIndexedSnapshot => "Grouped snapshots with hashed block comparisons for larger reads.",
            WatchBackendKind.SegmentedSnapshot =>
                "Snapshot reads that tolerate unreadable gaps by splitting the region.",
            WatchBackendKind.DirtyRange => "Cooperative producer publishes changed spans for fast wakeups.",
            WatchBackendKind.DirtyPage => "Dirty-page tracking driven by OS or engine support.",
            WatchBackendKind.SoftDirty => "Linux soft-dirty page tracking through /proc state.",
            WatchBackendKind.PageFault => "Debugger-mediated page-guard wakeups for low-latency activity hints.",
            WatchBackendKind.HardwareWatchpoint => "Per-thread hardware traps for the sharpest edge visibility.",
            WatchBackendKind.NativeAgent => "In-process cooperative helper inside the target process.",
            _ => "Generic memory observation strategy.",
        };

    private static string BuildCapabilityStatusSummary(
        MemoryWatchNegotiatedCapability capability,
        CapabilityConstraintDisplay constraint
    )
    {
        if (capability.CurrentRequestAvailable)
        {
            return capability.Backend switch
            {
                WatchBackendKind.Snapshot =>
                    "Ready now: OT2 can create this grouped snapshot watch on the attached target as-is.",
                WatchBackendKind.HashIndexedSnapshot =>
                    "Ready now: OT2 can create grouped hash-indexed snapshots on the attached target and wake decode only after hashed block changes are detected.",
                WatchBackendKind.SegmentedSnapshot =>
                    "Ready now: OT2 can create grouped segmented snapshots on the attached target and tolerate unreadable gaps by splitting the observed spans.",
                WatchBackendKind.PageFault =>
                    "Ready now: OT2 can arm page-backed wake watches, then refresh only the grouped snapshot domains those wake signals cover.",
                WatchBackendKind.DirtyPage =>
                    "Ready now: OT2 can consume page-dirty invalidation signals, then refresh only the grouped snapshot domains those signals touched.",
                WatchBackendKind.HardwareWatchpoint =>
                    "Ready now: OT2 can use this path by arming a dedicated wake sentinel in EEmem, then refreshing its grouped snapshots when that sentinel changes.",
                _ => "Ready now: OT2 can create this strategy for the current grouped EEmem watch as-is.",
            };
        }

        return constraint.Kind switch
        {
            MemoryCapabilityConstraintKind.NeedsAttachedTarget =>
                "Attach PCSX2 and rescan. OT2 needs a live EEmem target before it can validate this strategy.",
            MemoryCapabilityConstraintKind.NeedsIntrusiveOptIn =>
                "The host path is available, but OT2 will only request it after you explicitly allow intrusive backends.",
            MemoryCapabilityConstraintKind.NeedsOt2Helper =>
                "The host path exists in principle, but OT2 does not currently run the helper or producer this strategy requires.",
            MemoryCapabilityConstraintKind.NeedsProbeValidation =>
                "The packaged runtime includes this path, but the last live backend probe did not arm cleanly on the attached target.",
            MemoryCapabilityConstraintKind.NeedsOt2RuntimeIntegration =>
                "The host path exists in principle, but OT2's current runtime path does not surface the tracker plumbing this strategy needs.",
            MemoryCapabilityConstraintKind.NeedsThreadScopedIntegration =>
                "The host path exists, but this backend still wants thread-scoped scalar watches rather than OT2's normal grouped region request.",
            MemoryCapabilityConstraintKind.NotSupportedByOt2Model =>
                "The host path exists, but OT2's current grouped EEmem watch shape is still too coarse for this backend.",
            MemoryCapabilityConstraintKind.NotSupportedByHost =>
                "This host, runtime, packaged backend, or current rights cannot expose the strategy here.",
            _ => "This path is currently unavailable for the active OT2 MemoryWatcher request.",
        };
    }

    private static string BuildHostAvailabilitySummary(MemoryWatchNegotiatedCapability capability)
    {
        if (IsPackagedRuntimeSurfaceUnavailable(capability))
        {
            return capability.Backend switch
            {
                WatchBackendKind.DirtyPage =>
                    "Host verdict: the current packaged MemoryWatcher external runtime does not surface a dirty-page tracker backend for remote OT2 sessions.",
                _ =>
                    "Host verdict: the current packaged MemoryWatcher external runtime does not surface this backend for remote OT2 sessions.",
            };
        }

        if (IsLiveProbeFailure(capability.EnvironmentSupportReason))
        {
            return $"Host verdict: {capability.EnvironmentSupportReason}";
        }

        return capability.EnvironmentSupport switch
        {
            MemoryCapabilitySupportLevel.Supported => capability.Backend switch
            {
                WatchBackendKind.HardwareWatchpoint =>
                    "Host verdict: the OS/runtime can arm debugger-mediated hardware watchpoints on a compatible probe for the attached target.",
                WatchBackendKind.PageFault =>
                    "Host verdict: the packaged runtime can arm debugger-mediated PAGE_GUARD page watches on a backend-appropriate probe for the attached target.",
                _ =>
                    "Host verdict: this backend can be created on a backend-appropriate probe for the attached target.",
            },
            MemoryCapabilitySupportLevel.Conditional => capability.EnvironmentConstraintKind switch
            {
                RemoteConstraintKind.RequiresAgent or RemoteConstraintKind.RequiresCooperativeProducer =>
                    "Host verdict: this backend model exists, but it still needs a cooperative helper path.",
                RemoteConstraintKind.RequiresPageTracker =>
                    "Host verdict: this path depends on page-tracker support that is not surfaced for the current probe.",
                RemoteConstraintKind.TargetProcessMissing
                or RemoteConstraintKind.SessionUnavailable
                or RemoteConstraintKind.MissingRegion =>
                    "Host verdict: attach a live PCSX2 target to verify whether this backend can be used here.",
                _ => string.IsNullOrWhiteSpace(capability.EnvironmentSupportReason)
                    ? "Host verdict: this backend may work, but the host or target still needs extra setup."
                    : $"Host verdict: {capability.EnvironmentSupportReason}",
            },
            _ => string.IsNullOrWhiteSpace(capability.EnvironmentSupportReason)
                ? "Host verdict: this machine or runtime cannot expose this backend here."
                : $"Host verdict: {capability.EnvironmentSupportReason}",
        };
    }

    private static string? BuildCapabilityReason(
        MemoryWatchNegotiatedCapability capability,
        CapabilityConstraintDisplay constraint
    )
    {
        if (capability.CurrentRequestAvailable)
        {
            return null;
        }

        return constraint.Detail;
    }

    private static string BuildCapabilityPickWhen(MemoryWatchNegotiatedCapability capability, bool isRecommended) =>
        capability.Backend switch
        {
            WatchBackendKind.Snapshot when isRecommended =>
                "Best for: the safest default OT2 can usually rely on without extra hooks or helpers.",
            WatchBackendKind.Snapshot =>
                "Best for: lowest-risk grouped reads when you prefer stability over edge-exact timing.",
            WatchBackendKind.HashIndexedSnapshot =>
                "Best for: larger grouped reads where you want cheaper steady-state change detection without switching away from pure out-of-process snapshots.",
            WatchBackendKind.SegmentedSnapshot =>
                "Best for: grouped reads that may cross partially unreadable spans, where plain contiguous snapshots could fail outright.",
            WatchBackendKind.DirtyRange =>
                "Best for: cooperative emulator or helper integrations that can publish changed spans, not OT2's current plain external reader.",
            WatchBackendKind.DirtyPage =>
                "Best for: deployments that can emit page-level invalidation signals so OT2 can refresh only the grouped snapshot domains that were dirtied.",
            WatchBackendKind.SoftDirty =>
                "Best for: Linux-specific dirty tracking when you accept intrusive observation.",
            WatchBackendKind.PageFault =>
                "Best for: low-latency wakeups on read-hot pages when you want debugger-mediated signals to accelerate grouped snapshot refreshes and accept page-level over-wake churn.",
            WatchBackendKind.HardwareWatchpoint =>
                "Best for: the sharpest wake signal OT2 can use today by watching a tiny in-game sentinel, then refreshing grouped snapshots after it changes.",
            WatchBackendKind.NativeAgent =>
                "Best for: very low-latency observation when you are willing to run an in-process helper inside the target, which OT2 does not do today.",
            _ => "Best for: specialized scenarios where you explicitly want to force this observation path.",
        };

    private static string BuildRecommendedStrategyReason(MemoryWatchNegotiatedCapability capability) =>
        capability.Backend switch
        {
            WatchBackendKind.Snapshot =>
                "it is the least invasive ready-to-use choice and OT2 can rely on it without extra hooks or helpers.",
            WatchBackendKind.HashIndexedSnapshot =>
                "it stays fully out-of-process while reducing steady-state work on larger grouped snapshots through hashed block comparisons.",
            WatchBackendKind.SegmentedSnapshot =>
                "it stays fully out-of-process and tolerates unreadable gaps better than a plain contiguous snapshot request.",
            WatchBackendKind.HardwareWatchpoint =>
                "it currently exposes the strongest wake precision OT2 can use on this target, but it is more invasive and still rides on a tiny sentinel plus grouped refreshes.",
            WatchBackendKind.DirtyRange or WatchBackendKind.NativeAgent =>
                "it belongs to a cooperative helper path that could beat pure snapshots later, but OT2 does not run that deployment class yet.",
            _ => "it is the best currently matching balance of availability, invasiveness, precision, and latency.",
        };

    private static MemoryWatchNegotiatedCapability? SelectHigherPrecisionCapability(
        IReadOnlyList<MemoryWatchNegotiatedCapability> capabilities
    ) =>
        capabilities
            .Where(capability =>
                capability.EnvironmentSupport
                    is MemoryCapabilitySupportLevel.Supported
                        or MemoryCapabilitySupportLevel.Conditional
            )
            .OrderByDescending(static capability => GetPrecisionRank(capability.PrecisionClass))
            .ThenByDescending(static capability => capability.CurrentRequestAvailable)
            .ThenByDescending(static capability => GetLatencyRank(capability.LatencyClass))
            .FirstOrDefault();

    private static string BuildHigherPrecisionTradeoff(MemoryWatchNegotiatedCapability capability) =>
        capability.Backend switch
        {
            WatchBackendKind.HashIndexedSnapshot =>
                "Use it when your grouped snapshots are large enough that hash-based dirty narrowing is worth the extra index bookkeeping.",
            WatchBackendKind.SegmentedSnapshot =>
                "Use it when readability gaps matter more than the slightly simpler fast path of a plain contiguous snapshot.",
            WatchBackendKind.HardwareWatchpoint =>
                "Use it only when you explicitly want sharper transient-edge detection and accept intrusive thread-level setup.",
            WatchBackendKind.PageFault =>
                "Use it when debugger-mediated page guards are more valuable than pure polling, while accepting that hot pages can wake more often than the exact field you care about.",
            WatchBackendKind.DirtyRange or WatchBackendKind.NativeAgent =>
                "Use it when you have a cooperative helper path and want faster wakeups than plain snapshots.",
            _ => "Use it only when you explicitly want the tradeoffs of this more specialized path.",
        };

    private static MemoryWatchNegotiatedCapability? SelectBestReadyCapability(
        IReadOnlyList<MemoryWatchNegotiatedCapability> capabilities
    ) =>
        capabilities
            .Where(static capability => capability.CurrentRequestAvailable)
            .OrderBy(static capability => GetInvasivenessRank(capability.Invasiveness))
            .ThenByDescending(static capability => GetPrecisionRank(capability.PrecisionClass))
            .ThenByDescending(static capability => GetLatencyRank(capability.LatencyClass))
            .FirstOrDefault();

    private static MemoryWatchNegotiatedCapability? SelectBestConfigurableCapability(
        IReadOnlyList<MemoryWatchNegotiatedCapability> capabilities
    ) =>
        capabilities
            .Where(IsSelectableOt2Request)
            .OrderBy(static capability => GetInvasivenessRank(capability.Invasiveness))
            .ThenByDescending(static capability => GetPrecisionRank(capability.PrecisionClass))
            .ThenByDescending(static capability => GetLatencyRank(capability.LatencyClass))
            .FirstOrDefault();

    private static MemoryWatchNegotiatedCapability? SelectHigherPrecisionSelectableCapability(
        IReadOnlyList<MemoryWatchNegotiatedCapability> capabilities
    ) =>
        capabilities
            .Where(IsSelectableOt2Request)
            .OrderByDescending(static capability => GetPrecisionRank(capability.PrecisionClass))
            .ThenByDescending(static capability => GetLatencyRank(capability.LatencyClass))
            .ThenBy(static capability => GetInvasivenessRank(capability.Invasiveness))
            .FirstOrDefault();

    private static CapabilityConstraintDisplay DescribeCapabilityConstraint(MemoryWatchNegotiatedCapability capability)
    {
        if (capability.CurrentRequestAvailable)
        {
            return new CapabilityConstraintDisplay(
                MemoryCapabilityConstraintKind.None,
                string.Empty,
                null,
                true,
                "Use This Strategy"
            );
        }

        string rawReason = GetRawCapabilityReason(capability);
        RemoteConstraintKind currentConstraint = capability.CurrentRequestConstraintKind;
        RemoteConstraintKind environmentConstraint = capability.EnvironmentConstraintKind;

        if (
            currentConstraint
                is RemoteConstraintKind.UnsupportedHostPlatform
                    or RemoteConstraintKind.UnsupportedPackagedRuntime
            || environmentConstraint
                is RemoteConstraintKind.UnsupportedHostPlatform
                    or RemoteConstraintKind.UnsupportedPackagedRuntime
            || capability.EnvironmentSupport == MemoryCapabilitySupportLevel.Unsupported
        )
        {
            return new CapabilityConstraintDisplay(
                MemoryCapabilityConstraintKind.NotSupportedByHost,
                "Not supported on this host",
                string.IsNullOrWhiteSpace(rawReason)
                    ? "This host, runtime, packaged backend, or current rights cannot expose the strategy here."
                    : rawReason,
                false,
                "Unsupported Here"
            );
        }

        if (currentConstraint == RemoteConstraintKind.MissingIntrusiveOptIn)
        {
            return new CapabilityConstraintDisplay(
                MemoryCapabilityConstraintKind.NeedsIntrusiveOptIn,
                "Enable intrusive backends",
                "OT2 can request this path after you enable 'Allow intrusive backends'. The card action will do that automatically.",
                true,
                "Enable And Use"
            );
        }

        if (currentConstraint == RemoteConstraintKind.MissingHardwareThreadIds)
        {
            return new CapabilityConstraintDisplay(
                MemoryCapabilityConstraintKind.NeedsThreadScopedIntegration,
                "Needs thread-scoped watch fit",
                "This path arms per-thread traps. OT2 would need dedicated scalar point watches, not just grouped EEmem regions, before this becomes a clean fit.",
                false,
                "Needs Point-Watch Mode"
            );
        }

        if (currentConstraint == RemoteConstraintKind.Unknown && IsLiveProbeFailure(rawReason))
        {
            return new CapabilityConstraintDisplay(
                MemoryCapabilityConstraintKind.NeedsProbeValidation,
                "Live probe did not arm",
                rawReason,
                true,
                "Try This Strategy"
            );
        }

        if (
            currentConstraint
            is RemoteConstraintKind.RequiresScalarAlignedRegion
                or RemoteConstraintKind.UnsupportedIntent
        )
        {
            return new CapabilityConstraintDisplay(
                MemoryCapabilityConstraintKind.NotSupportedByOt2Model,
                "Needs finer watch shape",
                "OT2 currently watches grouped EEmem regions, while this backend expects tiny aligned scalar addresses. Supporting it needs a hybrid point-watch model, not just a small wiring tweak.",
                false,
                "Needs Point-Watch Mode"
            );
        }

        if (currentConstraint == RemoteConstraintKind.BackendNotSurfacedForRegion)
        {
            return DescribeUnsurfacedBackendConstraint(capability, rawReason);
        }

        if (currentConstraint is RemoteConstraintKind.RequiresAgent or RemoteConstraintKind.RequiresCooperativeProducer)
        {
            return new CapabilityConstraintDisplay(
                MemoryCapabilityConstraintKind.NeedsOt2Helper,
                capability.Backend == WatchBackendKind.NativeAgent
                    ? "Needs in-process helper"
                    : "Needs cooperative helper",
                capability.Backend == WatchBackendKind.NativeAgent
                    ? "This strategy needs an in-process helper inside the target process, and OT2 does not run that helper today."
                    : "This strategy needs a cooperative producer that can publish changed spans. OT2 does not provide that helper path today.",
                false,
                "Needs Helper Path"
            );
        }

        if (currentConstraint == RemoteConstraintKind.RequiresPageTracker)
        {
            return new CapabilityConstraintDisplay(
                MemoryCapabilityConstraintKind.NeedsOt2RuntimeIntegration,
                "Needs page-tracker runtime",
                "This strategy needs page-dirty or page-fault tracker plumbing that OT2's current external grouped session does not expose.",
                false,
                "Not Wired In OT2"
            );
        }

        if (
            environmentConstraint
            is RemoteConstraintKind.RequiresAgent
                or RemoteConstraintKind.RequiresCooperativeProducer
        )
        {
            return new CapabilityConstraintDisplay(
                MemoryCapabilityConstraintKind.NeedsOt2Helper,
                capability.Backend == WatchBackendKind.NativeAgent
                    ? "Needs in-process helper"
                    : "Needs cooperative helper",
                string.IsNullOrWhiteSpace(rawReason)
                    ? "Host support exists in principle, but OT2 does not currently provide the cooperative helper this path expects."
                    : rawReason,
                false,
                "Needs Helper Path"
            );
        }

        if (environmentConstraint == RemoteConstraintKind.RequiresPageTracker)
        {
            return new CapabilityConstraintDisplay(
                MemoryCapabilityConstraintKind.NeedsOt2RuntimeIntegration,
                "Needs page-tracker runtime",
                string.IsNullOrWhiteSpace(rawReason)
                    ? "Host support exists in principle, but OT2 does not yet expose the page-tracker plumbing this strategy expects."
                    : rawReason,
                false,
                "Not Wired In OT2"
            );
        }

        if (
            currentConstraint
            is RemoteConstraintKind.TargetProcessMissing
                or RemoteConstraintKind.SessionUnavailable
                or RemoteConstraintKind.MissingRegion
        )
        {
            return new CapabilityConstraintDisplay(
                MemoryCapabilityConstraintKind.NeedsAttachedTarget,
                "Needs live PCSX2 target",
                currentConstraint == RemoteConstraintKind.MissingRegion
                    ? "PCSX2 is attached, but OT2 could not resolve a live EEmem-backed region for this strategy yet. Let the emulator finish booting, then rescan."
                    : "Attach PCSX2 and rescan so OT2 can negotiate this strategy against a real grouped EEmem watch instead of a detached probe.",
                true,
                "Use And Validate Later"
            );
        }

        return new CapabilityConstraintDisplay(
            MemoryCapabilityConstraintKind.NeedsOt2RuntimeIntegration,
            "Needs runtime path",
            string.IsNullOrWhiteSpace(rawReason)
                ? "Host support exists in principle, but OT2 does not yet expose the exact integration this strategy needs."
                : rawReason,
            false,
            "Not Wired In OT2"
        );
    }

    private static CapabilityConstraintDisplay DescribeUnsurfacedBackendConstraint(
        MemoryWatchNegotiatedCapability capability,
        string rawReason
    )
    {
        return capability.Backend switch
        {
            WatchBackendKind.DirtyRange => new CapabilityConstraintDisplay(
                MemoryCapabilityConstraintKind.NeedsOt2Helper,
                "Needs cooperative helper",
                "OT2 is not currently connected to a cooperative dirty-range producer for the grouped EEmem regions it watches.",
                false,
                "Needs Helper Path"
            ),
            WatchBackendKind.DirtyPage => new CapabilityConstraintDisplay(
                MemoryCapabilityConstraintKind.NotSupportedByHost,
                "Unavailable in packaged runtime",
                "The packaged MemoryWatcher runtime does not currently surface dirty-page tracking for OT2's grouped EEmem request.",
                false,
                "Unsupported Here"
            ),
            WatchBackendKind.PageFault => new CapabilityConstraintDisplay(
                MemoryCapabilityConstraintKind.NeedsProbeValidation,
                "Probe did not arm backend",
                string.IsNullOrWhiteSpace(rawReason)
                    ? "The packaged page-fault path exists, but the last live page probe did not arm a usable backend for OT2's grouped EEmem request."
                    : rawReason,
                true,
                "Try This Strategy"
            ),
            WatchBackendKind.NativeAgent => new CapabilityConstraintDisplay(
                MemoryCapabilityConstraintKind.NeedsOt2Helper,
                "Needs in-process helper",
                "OT2 is not currently running the in-process helper path this strategy expects for the grouped EEmem regions.",
                false,
                "Needs Helper Path"
            ),
            WatchBackendKind.HardwareWatchpoint => new CapabilityConstraintDisplay(
                MemoryCapabilityConstraintKind.NeedsThreadScopedIntegration,
                "Needs finer watch shape",
                "The host can negotiate watchpoints on tiny scalar units, but OT2's current grouped-region request is not shaped like that yet.",
                false,
                "Needs Point-Watch Mode"
            ),
            _ => new CapabilityConstraintDisplay(
                MemoryCapabilityConstraintKind.NeedsOt2RuntimeIntegration,
                "Needs runtime path",
                string.IsNullOrWhiteSpace(rawReason)
                    ? "The current OT2 grouped-region request did not expose a usable implementation for this strategy."
                    : rawReason,
                false,
                "Not Wired In OT2"
            ),
        };
    }

    private static string GetRawCapabilityReason(MemoryWatchNegotiatedCapability capability) =>
        capability.CurrentRequestReason ?? capability.EnvironmentSupportReason ?? string.Empty;

    private static string? MaybePrefix(string prefix, string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : $"{prefix}{value}";

    private static bool RequiresIntrusiveOptIn(WatchBackendKind backend) =>
        backend
            is WatchBackendKind.HardwareWatchpoint
                or WatchBackendKind.SoftDirty
                or WatchBackendKind.PageFault
                or WatchBackendKind.DirtyPage;

    private static string JoinLines(params string?[] lines)
    {
        List<string> filtered = new(lines.Length);
        foreach (string? line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                filtered.Add(line);
            }
        }

        return string.Join(Environment.NewLine, filtered);
    }

    private static MemoryWatchNegotiatedCapability? FindRequestedCapability(
        IReadOnlyList<MemoryWatchNegotiatedCapability> capabilities,
        OutbreakTrackerSettings settings
    )
    {
        MemoryWatcherSettings memoryWatcher = settings.MemoryWatcher;
        if (capabilities.Count == 0)
        {
            return null;
        }

        if (memoryWatcher.PreferredBackend != WatchBackendKind.Auto)
        {
            MemoryWatchNegotiatedCapability? exact = capabilities.FirstOrDefault(capability =>
                capability.Backend == memoryWatcher.PreferredBackend
            );
            if (exact is not null)
            {
                return exact;
            }

            return memoryWatcher.AllowFallback ? SelectBestCapability(capabilities) : null;
        }

        MemoryWatchNegotiatedCapability? precisionMatch = capabilities
            .Where(capability => GetEffectivePrecision(capability) == memoryWatcher.PreferredPrecision)
            .OrderByDescending(static capability => capability.CurrentRequestAvailable)
            .ThenByDescending(static capability =>
                capability.EnvironmentSupport == MemoryCapabilitySupportLevel.Supported
            )
            .ThenBy(static capability => GetInvasivenessRank(capability.Invasiveness))
            .ThenByDescending(static capability => GetLatencyRank(capability.LatencyClass))
            .FirstOrDefault();

        return precisionMatch ?? (memoryWatcher.AllowFallback ? SelectBestCapability(capabilities) : null);
    }

    private static MemoryWatchNegotiatedCapability SelectBestCapability(
        IReadOnlyList<MemoryWatchNegotiatedCapability> capabilities
    ) =>
        capabilities
            .OrderByDescending(static capability => capability.CurrentRequestAvailable)
            .ThenByDescending(static capability =>
                capability.EnvironmentSupport == MemoryCapabilitySupportLevel.Supported
            )
            .ThenBy(static capability => GetInvasivenessRank(capability.Invasiveness))
            .ThenByDescending(static capability => GetPrecisionRank(capability.PrecisionClass))
            .ThenByDescending(static capability => GetLatencyRank(capability.LatencyClass))
            .First();

    private static WatchPrecision GetEffectivePrecision(MemoryWatchNegotiatedCapability capability) =>
        capability.CurrentCapability?.Precision ?? GetDefaultPrecisionForBackend(capability.Backend);

    private static bool TryGetFixedPrecisionForBackend(WatchBackendKind backend, out WatchPrecision precision)
    {
        if (backend == WatchBackendKind.Auto)
        {
            precision = WatchPrecision.SnapshotBitExact;
            return false;
        }

        precision = GetDefaultPrecisionForBackend(backend);
        return true;
    }

    private static WatchPrecision GetDefaultPrecisionForBackend(WatchBackendKind backend)
    {
        return backend switch
        {
            WatchBackendKind.Snapshot or WatchBackendKind.HashIndexedSnapshot or WatchBackendKind.SegmentedSnapshot =>
                WatchPrecision.SnapshotBitExact,
            WatchBackendKind.DirtyPage => WatchPrecision.DirtyPageThenBitDiff,
            WatchBackendKind.SoftDirty => WatchPrecision.SoftDirtyThenBitDiff,
            WatchBackendKind.PageFault => WatchPrecision.PageFaultThenBitDiff,
            WatchBackendKind.HardwareWatchpoint => WatchPrecision.HardwareAddressExact,
            WatchBackendKind.NativeAgent or WatchBackendKind.DirtyRange => WatchPrecision.DirtyRangeThenBitDiff,
            _ => WatchPrecision.SnapshotBitExact,
        };
    }

    private static string NormalizeFilter(string? value) => value?.Trim() ?? string.Empty;

    private static string? NormalizeOptionalText(string? value)
    {
        string normalized = NormalizeFilter(value);
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    private static string NormalizeScenarioSelection(string? value)
    {
        string normalized = NormalizeFilter(value);
        if (string.IsNullOrEmpty(normalized))
        {
            return string.Empty;
        }

        if (TryGetScenarioDisplayName(normalized, out string scenarioDisplayName))
        {
            return scenarioDisplayName;
        }

        string[] partialMatches = Array.FindAll(
            LobbyScenarioOptionValues,
            option => option.Contains(normalized, StringComparison.OrdinalIgnoreCase)
        );

        return partialMatches.Length == 1 ? partialMatches[0] : normalized;
    }

    private static bool TryGetScenarioDisplayName(string value, out string displayName)
    {
        if (EnumUtility.TryParseByValueOrMember(value, out Scenario scenario) && scenario != Scenario.Unknown)
        {
            displayName = EnumUtility.GetEnumString(scenario, Scenario.Unknown);
            return true;
        }

        displayName = string.Empty;
        return false;
    }

    private static string[] CreateLobbyScenarioOptions()
    {
        ImmutableArray<Scenario> scenarios = FastEnum.GetValues<Scenario>();
        string[] options = new string[scenarios.Length - 1];
        int index = 0;

        foreach (Scenario scenario in scenarios)
        {
            if (scenario == Scenario.Unknown)
            {
                continue;
            }

            options[index++] = EnumUtility.GetEnumString(scenario, Scenario.Unknown);
        }

        return options;
    }

    private static MemoryWatcherBackendOption[] CreatePreferredMemoryWatcherBackendOptions() =>
        [
            new(
                WatchBackendKind.Auto,
                "Automatic",
                "Let MemoryWatcher pick the best matching strategy from the requested precision and fallback policy."
            ),
            new(WatchBackendKind.Snapshot, "Snapshot", "Plain contiguous snapshot reads."),
            new(
                WatchBackendKind.HashIndexedSnapshot,
                "Hash Indexed Snapshot",
                "Snapshot reads with hash-indexed dirty detection for larger grouped regions."
            ),
            new(
                WatchBackendKind.SegmentedSnapshot,
                "Segmented Snapshot",
                "Snapshot reads that tolerate unreadable gaps by splitting the region."
            ),
            new(WatchBackendKind.DirtyPage, "Dirty Page", "OS-mediated page-dirty wakeups followed by bit diffing."),
            new(WatchBackendKind.SoftDirty, "Soft Dirty", "Linux soft-dirty page tracking."),
            new(WatchBackendKind.PageFault, "Page Fault", "Page-fault driven wakeups."),
            new(
                WatchBackendKind.HardwareWatchpoint,
                "Hardware Watchpoint",
                "Per-thread hardware watchpoints for the sharpest edge detection."
            ),
            new(WatchBackendKind.NativeAgent, "Native Agent", "Cooperative in-process helper path."),
            new(WatchBackendKind.DirtyRange, "Dirty Range", "Cooperative changed-range publication."),
        ];

    private static MemoryWatcherPrecisionOption[] CreatePreferredMemoryWatcherPrecisionOptions() =>
        [
            new(
                WatchPrecision.SnapshotBitExact,
                "Snapshot Bit Exact",
                "Final sampled value after grouped snapshot reads."
            ),
            new(
                WatchPrecision.DirtyPageThenBitDiff,
                "Dirty Page Then Bit Diff",
                "OS-reported dirty pages with a final diff pass."
            ),
            new(
                WatchPrecision.SoftDirtyThenBitDiff,
                "Soft Dirty Then Bit Diff",
                "Linux soft-dirty pages with a final diff pass."
            ),
            new(
                WatchPrecision.PageFaultThenBitDiff,
                "Page Fault Then Bit Diff",
                "Page-fault wakeups with a final diff pass."
            ),
            new(
                WatchPrecision.DirtyRangeThenBitDiff,
                "Dirty Range Then Bit Diff",
                "Cooperatively published changed spans with a final diff pass."
            ),
            new(
                WatchPrecision.HardwareAddressExact,
                "Hardware Address Exact",
                "Hardware trap precision for precise edge visibility on watched units."
            ),
        ];

    private static string GetMemoryBackendModeLabel(MemoryBackendMode mode) =>
        mode switch
        {
            MemoryBackendMode.Legacy => "Legacy",
            MemoryBackendMode.MemoryWatcher => "MemoryWatcher",
            _ => mode.ToString(),
        };

    private static string GetWatchBackendLabel(WatchBackendKind backend) =>
        backend switch
        {
            WatchBackendKind.Auto => "Automatic",
            WatchBackendKind.HashIndexedSnapshot => "Hash Indexed Snapshot",
            WatchBackendKind.SegmentedSnapshot => "Segmented Snapshot",
            WatchBackendKind.DirtyPage => "Dirty Page",
            WatchBackendKind.SoftDirty => "Soft Dirty",
            WatchBackendKind.PageFault => "Page Fault",
            WatchBackendKind.HardwareWatchpoint => "Hardware Watchpoint",
            WatchBackendKind.NativeAgent => "Native Agent",
            WatchBackendKind.DirtyRange => "Dirty Range",
            _ => backend.ToString(),
        };

    private static string GetWatchPrecisionLabel(WatchPrecision precision) =>
        precision switch
        {
            WatchPrecision.SnapshotBitExact => "Snapshot Bit Exact",
            WatchPrecision.HardwareAddressExact => "Hardware Address Exact",
            WatchPrecision.PageFaultThenBitDiff => "Page Fault Then Bit Diff",
            WatchPrecision.SoftDirtyThenBitDiff => "Soft Dirty Then Bit Diff",
            WatchPrecision.DirtyPageThenBitDiff => "Dirty Page Then Bit Diff",
            WatchPrecision.DirtyRangeThenBitDiff => "Dirty Range Then Bit Diff",
            _ => precision.ToString(),
        };

    private static string FormatSupport(MemoryCapabilitySupportLevel support) =>
        support switch
        {
            MemoryCapabilitySupportLevel.Supported => "Supported",
            MemoryCapabilitySupportLevel.Conditional => "Conditional",
            _ => "Unsupported",
        };

    private static string FormatInvasiveness(MemoryObservationInvasiveness invasiveness) =>
        invasiveness switch
        {
            MemoryObservationInvasiveness.OutOfProcess => "Out of process",
            MemoryObservationInvasiveness.OperatingSystemHook => "OS hook",
            MemoryObservationInvasiveness.ExecutableHook => "Executable hook",
            MemoryObservationInvasiveness.KernelHook => "Kernel hook",
            _ => invasiveness.ToString(),
        };

    private static string FormatPrecision(MemoryObservationPrecisionClass precision) =>
        precision switch
        {
            MemoryObservationPrecisionClass.SampledFinalValue => "Sampled final value",
            MemoryObservationPrecisionClass.SignaledFinalValue => "Signaled final value",
            MemoryObservationPrecisionClass.TransientEdgeExact => "Transient edge exact",
            _ => precision.ToString(),
        };

    private static string FormatLatency(MemoryObservationLatencyClass latency) =>
        latency switch
        {
            MemoryObservationLatencyClass.UnknownOrCallerDriven => "Caller driven",
            MemoryObservationLatencyClass.OverOrEqual100Milliseconds => ">= 100 ms",
            MemoryObservationLatencyClass.Under100Milliseconds => "< 100 ms",
            MemoryObservationLatencyClass.Under1Millisecond => "< 1 ms",
            MemoryObservationLatencyClass.Under100Nanoseconds => "< 100 ns",
            MemoryObservationLatencyClass.Under50Nanoseconds => "< 50 ns",
            _ => latency.ToString(),
        };

    private static int GetInvasivenessRank(MemoryObservationInvasiveness invasiveness) =>
        invasiveness switch
        {
            MemoryObservationInvasiveness.OutOfProcess => 0,
            MemoryObservationInvasiveness.OperatingSystemHook => 1,
            MemoryObservationInvasiveness.ExecutableHook => 2,
            MemoryObservationInvasiveness.KernelHook => 3,
            _ => 4,
        };

    private static int GetPrecisionRank(MemoryObservationPrecisionClass precision) =>
        precision switch
        {
            MemoryObservationPrecisionClass.TransientEdgeExact => 2,
            MemoryObservationPrecisionClass.SignaledFinalValue => 1,
            _ => 0,
        };

    private static int GetLatencyRank(MemoryObservationLatencyClass latency) =>
        latency switch
        {
            MemoryObservationLatencyClass.Under50Nanoseconds => 5,
            MemoryObservationLatencyClass.Under100Nanoseconds => 4,
            MemoryObservationLatencyClass.Under1Millisecond => 3,
            MemoryObservationLatencyClass.Under100Milliseconds => 2,
            MemoryObservationLatencyClass.OverOrEqual100Milliseconds => 1,
            _ => 0,
        };

    public sealed record MemoryBackendCard(
        MemoryBackendMode Mode,
        string Title,
        string Summary,
        CapabilityStatusDisplay Status,
        string Metrics,
        string StatusSummary,
        string? Reason,
        string PickWhen,
        bool IsSelected,
        bool IsRecommended
    )
    {
        public string SupportLabel => Status.Label;

        public IBrush SupportBadgeBackgroundBrush => Status.BackgroundBrush;

        public IBrush SupportBadgeBorderBrush => Status.BorderBrush;

        public IBrush SupportBadgeForegroundBrush => Status.ForegroundBrush;

        public bool HasReason => !string.IsNullOrWhiteSpace(Reason);

        public bool HasStatusSummary => !string.IsNullOrWhiteSpace(StatusSummary);

        public bool HasPickWhen => !string.IsNullOrWhiteSpace(PickWhen);

        public bool HasRecommendationLabel => IsRecommended;

        public string RecommendationLabel => IsRecommended ? "Recommended default" : string.Empty;

        public string ButtonText => IsSelected ? "Selected" : $"Use {Title}";
    }

    public sealed record MemoryWatcherCapabilityCard(
        WatchBackendKind Backend,
        WatchPrecision Precision,
        string Title,
        string Subtitle,
        CapabilityStatusDisplay HostStatus,
        CapabilityStatusDisplay Status,
        MemoryCapabilityConstraintKind ConstraintKind,
        string HostSummary,
        string ConstraintLabel,
        string StatusSummary,
        string Metrics,
        string? Reason,
        string PickWhen,
        string ActionLabel,
        bool CanApply,
        bool IsSelected,
        bool IsRecommended
    )
    {
        public string HostSupportLabel => $"Host: {HostStatus.Label}";

        public IBrush HostSupportBadgeBackgroundBrush => HostStatus.BackgroundBrush;

        public IBrush HostSupportBadgeBorderBrush => HostStatus.BorderBrush;

        public IBrush HostSupportBadgeForegroundBrush => HostStatus.ForegroundBrush;

        public string SupportLabel => $"OT2: {Status.Label}";

        public IBrush SupportBadgeBackgroundBrush => Status.BackgroundBrush;

        public IBrush SupportBadgeBorderBrush => Status.BorderBrush;

        public IBrush SupportBadgeForegroundBrush => Status.ForegroundBrush;

        public bool HasHostSummary => !string.IsNullOrWhiteSpace(HostSummary);

        public bool HasReason => !string.IsNullOrWhiteSpace(Reason);

        public bool HasConstraintLabel => !string.IsNullOrWhiteSpace(ConstraintLabel);

        public bool HasPickWhen => !string.IsNullOrWhiteSpace(PickWhen);

        public bool HasRecommendationLabel => IsRecommended;

        public string RecommendationLabel => IsRecommended ? "Recommended default" : string.Empty;

        public string ButtonText =>
            IsSelected ? "Selected Request"
            : IsRecommended ? "Use Recommended Default"
            : ActionLabel;
    }

    public sealed record CapabilityStatusDisplay(
        MemoryCapabilityDisplayKind Kind,
        string Label,
        IBrush BackgroundBrush,
        IBrush BorderBrush,
        IBrush ForegroundBrush
    );

    public enum MemoryCapabilityDisplayKind
    {
        Ready = 0,
        NeedsWork = 1,
        Unsupported = 2,
    }

    private enum MemoryCapabilityBucket
    {
        Ready = 0,
        Actionable = 1,
        Future = 2,
        Unavailable = 3,
    }

    public enum MemoryCapabilityConstraintKind
    {
        None = 0,
        NeedsAttachedTarget = 1,
        NeedsIntrusiveOptIn = 2,
        NeedsOt2Helper = 3,
        NeedsProbeValidation = 4,
        NeedsOt2RuntimeIntegration = 5,
        NeedsThreadScopedIntegration = 6,
        NotSupportedByOt2Model = 7,
        NotSupportedByHost = 8,
    }

    public sealed record CapabilityConstraintDisplay(
        MemoryCapabilityConstraintKind Kind,
        string Label,
        string? Detail,
        bool CanApply,
        string ButtonText
    );

    public sealed record MemoryWatcherBackendOption(
        WatchBackendKind Value,
        string Label,
        string Description,
        CapabilityStatusDisplay? Status = null,
        string StateSummary = ""
    )
    {
        public bool HasStatus => Status is not null;

        public string StatusLabel => Status?.Label ?? string.Empty;

        public IBrush? StatusBadgeBackgroundBrush => Status?.BackgroundBrush;

        public IBrush? StatusBadgeBorderBrush => Status?.BorderBrush;

        public IBrush? StatusBadgeForegroundBrush => Status?.ForegroundBrush;

        public bool HasStateSummary => !string.IsNullOrWhiteSpace(StateSummary);
    }

    public sealed record MemoryWatcherPrecisionOption(
        WatchPrecision Value,
        string Label,
        string Description,
        CapabilityStatusDisplay? Status = null,
        string StateSummary = ""
    )
    {
        public bool HasStatus => Status is not null;

        public string StatusLabel => Status?.Label ?? string.Empty;

        public IBrush? StatusBadgeBackgroundBrush => Status?.BackgroundBrush;

        public IBrush? StatusBadgeBorderBrush => Status?.BorderBrush;

        public IBrush? StatusBadgeForegroundBrush => Status?.ForegroundBrush;

        public bool HasStateSummary => !string.IsNullOrWhiteSpace(StateSummary);
    }

    private void QueueToast(NotificationType type, string title, string content) =>
        _toastManager.CreateSimpleInfoToast().OfType(type).WithTitle(title).WithContent(content).Queue();
}
