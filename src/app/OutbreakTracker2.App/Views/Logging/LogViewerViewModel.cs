using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using OutbreakTracker2.App.Services;
using OutbreakTracker2.App.Services.Dispatcher;
using OutbreakTracker2.App.Services.LogStorage;
using OutbreakTracker2.Extensions;
using R3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OutbreakTracker2.App.Views.Logging;

public partial class LogViewerViewModel : ObservableObject, IDisposable
{
    private readonly ClipboardService _clipboardService;
    private readonly IDispatcherService _dispatcherService;
    private readonly ILogDataStorageService _dataStorageService;
    private readonly ILogger<LogViewerViewModel> _logger;
    private readonly Subject<Unit> _filterUpdateSubject = new();
    private readonly IDisposable? _filterSubscription;
    private const int MaxLogEntries = 100;

    private readonly ObservableList<LogModel> _filteredEntries = [];
    public NotifyCollectionChangedSynchronizedViewList<LogModel> FilteredEntriesView { get; }

    [ObservableProperty]
    private LogModel? _logEntryToScrollTo;

    [ObservableProperty]
    private int _errorsCount;

    [ObservableProperty]
    private int _warningsCount;

    [ObservableProperty]
    private int _informationCount;

    [ObservableProperty]
    private int _criticalCount;

    [ObservableProperty]
    private int _traceCount;

    [ObservableProperty]
    private int _debugCount;

    [ObservableProperty]
    private HashSet<LogLevel> _selectedLogLevels = [];

    [ObservableProperty]
    private bool _autoScroll;

    [ObservableProperty]
    private bool _copyOnSelect;

    public bool IsTraceSelected
    {
        get => SelectedLogLevels.Contains(LogLevel.Trace);
        set => UpdateSelectedLogLevel(LogLevel.Trace, value);
    }

    public bool IsDebugSelected
    {
        get => SelectedLogLevels.Contains(LogLevel.Debug);
        set => UpdateSelectedLogLevel(LogLevel.Debug, value);
    }

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

    public LogModel? SelectedLogItem
    {
        get => _selectedLogItem;
        set
        {
            if (_selectedLogItem == value)
                return;

            _selectedLogItem = value;
            OnPropertyChanged();
            if (CopyOnSelect)
                CopySelectedLog();
        }
    }

    private LogModel? _selectedLogItem;

    public LogViewerViewModel(
        ILogDataStorageService dataStoreService,
        ClipboardService clipboardService,
        IDispatcherService dispatcherService,
        ILogger<LogViewerViewModel> logger)
    {
        _dataStorageService = dataStoreService;
        _clipboardService = clipboardService;
        _dispatcherService = dispatcherService;
        _logger = logger;

        _logger.LogInformation("LogViewerViewModel initialized");

        FilteredEntriesView = _filteredEntries
            .ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

        _filterSubscription = _filterUpdateSubject
            .Debounce(TimeSpan.FromMilliseconds(100))
            .SubscribeOnThreadPool()
            .SubscribeAwait((_, ct) => ApplyLogFiltersAsync(ct), AwaitOperation.Drop);

        if (_dataStorageService.Entries.Count is 0)
            _logger.LogWarning("Log data storage service entries are empty");

        _dataStorageService.Entries.CollectionChanged += OnLogCollectionChanged;
        _filterUpdateSubject.OnNext(Unit.Default);
    }

    private void OnLogCollectionChanged(in NotifyCollectionChangedEventArgs<LogModel> e)
    {
        _filterUpdateSubject.OnNext(Unit.Default);
    }

    private async ValueTask ApplyLogFiltersAsync(CancellationToken ct)
    {
        try
        {
            if (_dataStorageService.Entries.Count is 0)
                return;

            List<LogModel> currentEntriesSnapshot;
            lock (_dataStorageService.Entries.SyncRoot)
            {
                currentEntriesSnapshot = [.. _dataStorageService.Entries];
            }

            ct.ThrowIfCancellationRequested();

            UpdateLogCounts(currentEntriesSnapshot);

            ct.ThrowIfCancellationRequested();

            HashSet<LogLevel> selectedLevels = SelectedLogLevels.ToHashSet();
            IEnumerable<LogModel> filteredQuery = currentEntriesSnapshot;

            if (selectedLevels.Count > 0)
                filteredQuery = filteredQuery.Where(log => selectedLevels.Contains(log.LogLevel));

            List<LogModel> formattedAndFiltered = filteredQuery
                .Select(LogModel.ToDisplayForm)
                .TakeLast(MaxLogEntries)
                .ToList();

            ct.ThrowIfCancellationRequested();

            await _dispatcherService.InvokeOnUIAsync(() =>
            {
                _filteredEntries.ReplaceAll(formattedAndFiltered);

                if (AutoScroll && _filteredEntries.Count > 0)
                    LogEntryToScrollTo = _filteredEntries.Last();
                else
                    LogEntryToScrollTo = null;
            }, ct);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Log filtering operation was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during log filtering");
        }
    }

    private void UpdateLogCounts(List<LogModel> allEntries)
    {
        int errors = 0, warnings = 0, info = 0, critical = 0, trace = 0, debug = 0;

        foreach (LogModel log in allEntries)
            switch (log.LogLevel)
            {
                case LogLevel.Trace: trace++; break;
                case LogLevel.Debug: debug++; break;
                case LogLevel.Information: info++; break;
                case LogLevel.Warning: warnings++; break;
                case LogLevel.Error: errors++; break;
                case LogLevel.Critical: critical++; break;
                case LogLevel.None: break;
                default: _logger.LogWarning("Unknown LogLevel encountered: {LogLogLevel}", log.LogLevel); break;
            }

        _dispatcherService.PostOnUI(() =>
        {
            TraceCount = trace;
            DebugCount = debug;
            InformationCount = info;
            WarningsCount = warnings;
            ErrorsCount = errors;
            CriticalCount = critical;
        });
    }

    private void CopySelectedLog()
    {
        if (SelectedLogItem is null)
            return;

        string text = $"{SelectedLogItem.LogLevel}: {SelectedLogItem.DisplayMessage}";
        if (!string.IsNullOrEmpty(SelectedLogItem.DisplayException))
            text += Environment.NewLine + SelectedLogItem.DisplayException;

        _clipboardService.CopyToClipboard(text);
    }

    [RelayCommand]
    private void ToggleLogLevelFilter(LogLevel logLevel)
    {
        bool changed = SelectedLogLevels.Remove(logLevel);
        if (!changed)
            changed = SelectedLogLevels.Add(logLevel);

        OnPropertyChanged(nameof(IsTraceSelected));
        OnPropertyChanged(nameof(IsDebugSelected));
        OnPropertyChanged(nameof(IsInformationSelected));
        OnPropertyChanged(nameof(IsWarningSelected));
        OnPropertyChanged(nameof(IsErrorSelected));
        OnPropertyChanged(nameof(IsCriticalSelected));

        if (changed)
            _filterUpdateSubject.OnNext(Unit.Default);
    }

    private void UpdateSelectedLogLevel(LogLevel logLevel, bool isSelected)
    {
        bool changed = isSelected
            ? SelectedLogLevels.Add(logLevel)
            : SelectedLogLevels.Remove(logLevel);

        OnPropertyChanged(nameof(IsTraceSelected));
        OnPropertyChanged(nameof(IsDebugSelected));
        OnPropertyChanged(nameof(IsInformationSelected));
        OnPropertyChanged(nameof(IsWarningSelected));
        OnPropertyChanged(nameof(IsErrorSelected));
        OnPropertyChanged(nameof(IsCriticalSelected));

        if (changed)
            _filterUpdateSubject.OnNext(Unit.Default);
    }

    public void Dispose()
    {
        _filterSubscription?.Dispose();
        _filterUpdateSubject.Dispose();

        _dataStorageService.Entries.CollectionChanged -= OnLogCollectionChanged;

        _filteredEntries.Clear();
        SelectedLogLevels.Clear();

        _logger.LogDebug("LogViewerViewModel disposed. Subscriptions cancelled");
    }
}