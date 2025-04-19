using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using OutbreakTracker2.App.Services;
using R3;
using System.Collections.Generic;
using System.Linq;

namespace OutbreakTracker2.App.Views.Logging;

public partial class LogViewerViewModel : ObservableObject
{
    private readonly ClipboardService _clipboardService;
    private LogModel? _selectedLogItem;

    [ObservableProperty]
    private int _errorsCount;

    [ObservableProperty]
    private int _warningsCount;

    [ObservableProperty]
    private int _messagesCount;

    [ObservableProperty]
    private int _criticalCount;

    [ObservableProperty]
    private ObservableList<LogModel>? _filteredEntries;

    [ObservableProperty]
    private HashSet<LogLevel> _selectedLogLevels = [];

    public bool IsInformationSelected
    {
        get => SelectedLogLevels.Contains(LogLevel.Information);
        set => UpdateSelectedLogLevel(LogLevel.Information, value);
    }

    public bool IsWarningSelected
    {
        get => SelectedLogLevels.Contains(LogLevel.Warning);
        set => UpdateSelectedLogLevel(LogLevel.Warning, value);
    }

    public bool IsErrorSelected
    {
        get => SelectedLogLevels.Contains(LogLevel.Error);
        set => UpdateSelectedLogLevel(LogLevel.Error, value);
    }

    public bool IsCriticalSelected
    {
        get => SelectedLogLevels.Contains(LogLevel.Critical);
        set => UpdateSelectedLogLevel(LogLevel.Critical, value);
    }

    private ILogDataStorageService DataStore { get; set; }

    public bool AutoScroll { get; set; }
    public bool CopyOnSelect { get; set; }

    public LogModel? SelectedLogItem
    {
        get => _selectedLogItem;
        set
        {
            if (_selectedLogItem == value) return;

            _selectedLogItem = value;
            if (CopyOnSelect) CopySelectedLogAsync();
        }
    }

    public Observable<LogModel?>? ScrollToItem { get; set; }

    public LogViewerViewModel(
        ILogDataStorageService dataStore,
        ClipboardService clipboardService)
    {
        _clipboardService = clipboardService;
        DataStore = dataStore;
        AutoScroll = true;
        CopyOnSelect = false;

        if (DataStore.Entries is null) return;

        ErrorsCount = DataStore.Entries.Count(logModel => logModel.LogLevel is LogLevel.Error);
        WarningsCount = DataStore.Entries.Count(logModel => logModel.LogLevel is LogLevel.Warning);
        MessagesCount = DataStore.Entries.Count(logModel => logModel.LogLevel is LogLevel.Information);
        CriticalCount = DataStore.Entries.Count(logModel => logModel.LogLevel is LogLevel.Critical);

        DataStore.Entries.CollectionChanged += OnLogCollectionChanged;
    }

    private void OnLogCollectionChanged(in NotifyCollectionChangedEventArgs<LogModel> e)
    {
        if (DataStore.Entries is null) return;

        ErrorsCount = DataStore.Entries.Count(logModel => logModel.LogLevel is LogLevel.Error);
        WarningsCount = DataStore.Entries.Count(logModel => logModel.LogLevel is LogLevel.Warning);
        MessagesCount = DataStore.Entries.Count(logModel => logModel.LogLevel is LogLevel.Information);
        CriticalCount = DataStore.Entries.Count(logModel => logModel.LogLevel is LogLevel.Critical);

        ApplyLogFilters();
    }

    private void CopySelectedLogAsync()
    {
        if (SelectedLogItem?.State is not null)
        {
            var text = $"{SelectedLogItem.LogLevel}: {SelectedLogItem.State}";
            _clipboardService.CopyToClipboard(text);
        }
    }

    [RelayCommand]
    private void ToggleLogLevelFilter(LogLevel logLevel)
    {
        if (!SelectedLogLevels.Add(logLevel))
            SelectedLogLevels.Remove(logLevel);

        ApplyLogFilters();
    }

    private void ApplyLogFilters()
    {
        if (DataStore.Entries is null) return;

        if (SelectedLogLevels.Count is not 0)
            FilteredEntries = new ObservableList<LogModel>(DataStore.Entries
                .Where(log => SelectedLogLevels.Contains(log.LogLevel)));
        else
            FilteredEntries = new ObservableList<LogModel>(DataStore.Entries);
    }

    private void UpdateSelectedLogLevel(LogLevel logLevel, bool isSelected)
    {
        if (isSelected)
            SelectedLogLevels.Add(logLevel);
        else
            SelectedLogLevels.Remove(logLevel);

        ApplyLogFilters();
    }
}