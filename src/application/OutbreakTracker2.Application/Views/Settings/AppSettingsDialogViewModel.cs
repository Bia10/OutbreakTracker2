using System.Collections.Immutable;
using System.Diagnostics;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastEnumUtility;
using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Utility;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace OutbreakTracker2.Application.Views.Settings;

internal sealed partial class AppSettingsDialogViewModel : ObservableObject
{
    private static readonly string[] LobbyScenarioOptionValues = CreateLobbyScenarioOptions();

    private readonly IAppSettingsService _settingsService;
    private readonly ISukiToastManager _toastManager;
    private readonly ISukiDialog _dialog;
    private bool _isLoadingSettings;

    public string UserSettingsPath { get; }

    public IReadOnlyList<string> LobbyScenarioOptions { get; } = LobbyScenarioOptionValues;

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private string? _validationMessage;

    [ObservableProperty]
    private bool _enableToastAlerts;

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

    public AppSettingsDialogViewModel(
        IAppSettingsService settingsService,
        ISukiToastManager toastManager,
        ISukiDialog dialog
    )
    {
        _settingsService = settingsService;
        _toastManager = toastManager;
        _dialog = dialog;

        UserSettingsPath = settingsService.UserSettingsPath;

        Load(settingsService.Current);
        UpdateValidationMessage();
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        try
        {
            OutbreakTrackerSettings settings = BuildSettings();
            if (!settings.TryValidate(out string? error))
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

            EnableToastAlerts = notifications.EnableToastAlerts;

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

            DoorFlagChanged = doors.FlagChanged;
            DoorDestroyed = doors.Destroyed;
            DoorStatusChanged = doors.StatusChanged;

            LobbyGameCreated = lobby.GameCreated;
            LobbyNameMatchCreated = lobby.NameMatchCreated;
            LobbyNameMatchFilter = lobby.NameMatchFilter ?? string.Empty;
            LobbyScenarioMatchCreated = lobby.ScenarioMatchCreated;
            LobbyScenarioMatchFilter = NormalizeScenarioSelection(lobby.ScenarioMatchFilter);
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
                EntitiesDock = new EntitiesDockSettings
                {
                    OnlyShowCurrentPlayerRoom = EntitiesDockOnlyShowCurrentPlayerRoom,
                },
                ScenarioItemsDock = new ScenarioItemsDockSettings
                {
                    OnlyShowCurrentPlayerRoom = ScenarioItemsDockOnlyShowCurrentPlayerRoom,
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
        };

    private void UpdateValidationMessage()
    {
        if (_isLoadingSettings)
            return;

        OutbreakTrackerSettings settings = BuildSettings();
        ValidationMessage = settings.TryValidate(out string? error) ? null : error;
    }

    private static string NormalizeFilter(string? value) => value?.Trim() ?? string.Empty;

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

    private void QueueToast(NotificationType type, string title, string content) =>
        _toastManager.CreateSimpleInfoToast().OfType(type).WithTitle(title).WithContent(content).Queue();
}
