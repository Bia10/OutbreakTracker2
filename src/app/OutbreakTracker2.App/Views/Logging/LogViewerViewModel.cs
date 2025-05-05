using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using OutbreakTracker2.App.Services;
using OutbreakTracker2.App.Services.Dispatcher;
using OutbreakTracker2.App.Services.LogStorage;
using R3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZLinq;

namespace OutbreakTracker2.App.Views.Logging;

// TODO: this has been finally improved so much that the main performance bottleneck is the DataGrid itself.
// mainly the lack of virtualization and having to constantly remeasure/redraw thousands of rows per second + the autoscroll render priority ui work
// the next step at scaling the UI rendering performance would be some control supporting virtualization or caching of rows
public partial class LogViewerViewModel : ObservableObject, IDisposable
{
    private readonly IDisposable? _filterSubscription;
    private readonly ClipboardService _clipboardService;
    private readonly IDispatcherService _dispatcherService;
    private readonly ILogDataStorageService _dataStorageService;
    private readonly ILogger<LogViewerViewModel> _logger;
    private readonly Subject<Unit> _filterUpdateSubject = new();
    private readonly Lock _syncLock = new();
    private LogModel? _selectedLogItem;

    // Note: this basicaly acts as throttle on the ui thread pressure for now
    private const int _maxLogEntries = 100;

    private readonly ObservableList<LogModel> _filteredEntries = [];
    public NotifyCollectionChangedSynchronizedViewList<LogModel> FilteredEntriesView { get; set; }

    [ObservableProperty]
    private LogModel? _logEntryToScrollTo;

    [ObservableProperty]
    private int _errorsCount;

    [ObservableProperty]
    private int _warningsCount;

    [ObservableProperty]
    private int _messagesCount;

    [ObservableProperty]
    private int _criticalCount;

    [ObservableProperty]
    private int _traceCount;

    [ObservableProperty]
    private int _debugCount;

    [ObservableProperty]
    private HashSet<LogLevel> _selectedLogLevels = [];

    // Note: this will que tiny work on ui thread the more items there are the more time it takes
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

        FilteredEntriesView = _filteredEntries
            .ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

        _filterSubscription = _filterUpdateSubject
            .Debounce(TimeSpan.FromMilliseconds(50))
            .SubscribeOnThreadPool()
            .SubscribeAwait(async (_, ct) => await ApplyLogFiltersAsync(ct), AwaitOperation.Drop);

        if (_dataStorageService.Entries is null)
        {
            _logger.LogError("Log data storage service entries are null.");
            return;
        }

        _dataStorageService.Entries.CollectionChanged += OnLogCollectionChanged;
        _filterUpdateSubject.OnNext(Unit.Default);
    }

    private async ValueTask UpdateLogCountsAsync(List<LogModel> allEntries)
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

        await _dispatcherService.InvokeOnUIAsync(() =>
        {
            TraceCount = trace;
            DebugCount = debug;
            MessagesCount = info;
            WarningsCount = warnings;
            ErrorsCount = errors;
            CriticalCount = critical;
        });
    }

    private void OnLogCollectionChanged(in NotifyCollectionChangedEventArgs<LogModel> e)
        => _filterUpdateSubject.OnNext(Unit.Default);

    private async ValueTask ApplyLogFiltersAsync(CancellationToken ct)
    {
        try
        {
            if (_dataStorageService.Entries is null)
                return;

            List<LogModel> currentEntriesSnapshot;
            lock (_syncLock)
            {
                currentEntriesSnapshot = [.. _dataStorageService.Entries];
            }

            ct.ThrowIfCancellationRequested();

            await UpdateLogCountsAsync(currentEntriesSnapshot);

            ct.ThrowIfCancellationRequested();

            var selectedLevels = SelectedLogLevels.ToHashSet();
            IEnumerable<LogModel> filteredQuery = currentEntriesSnapshot;

            if (selectedLevels.Count > 0)
                filteredQuery = filteredQuery.Where(log => selectedLevels.Contains(log.LogLevel));

            var formattedAndFiltered = filteredQuery
                .AsValueEnumerable()
                .Select(LogModel.ToDisplayForm)
                .TakeLast(_maxLogEntries)
                .ToList();

            ct.ThrowIfCancellationRequested();

            await _dispatcherService.InvokeOnUIAsync(() =>
            {
                _filteredEntries.Clear();
                _filteredEntries.AddRange(formattedAndFiltered);

                if (AutoScroll && _filteredEntries.Count > 0)
                    LogEntryToScrollTo = _filteredEntries.Last();
                else
                    LogEntryToScrollTo = null;
            }, ct);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Log filtering operation was cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during log filtering");
        }
    }

    private void CopySelectedLog()
    {
        if (SelectedLogItem is null)
            return;

        var text = $"{SelectedLogItem.LogLevel}: {SelectedLogItem.DisplayMessage}";
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

        if (_dataStorageService.Entries is not null)
            _dataStorageService.Entries.CollectionChanged -= OnLogCollectionChanged;

        _filteredEntries.Clear();
        SelectedLogLevels.Clear();
    }
}