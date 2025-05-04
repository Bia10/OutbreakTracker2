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
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZLinq;

namespace OutbreakTracker2.App.Views.Logging;

public partial class LogViewerViewModel : ObservableObject, IDisposable
{
    private readonly IDisposable? _updateSubscription;
    private readonly ClipboardService _clipboardService;
    private readonly IDispatcherService _dispatcherService;
    private readonly Lock _syncLock = new();
    private LogModel? _selectedLogItem;
    private bool _isFilteringInProgress;
    private const int _maxLogEntries = 10000;
    private CancellationTokenSource? _filterCancellationSource;

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

    [ObservableProperty]
    private bool _autoScroll = true;

    [ObservableProperty]
    private bool _copyOnSelect;

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
        ClipboardService clipboardService,
        IDispatcherService dispatcherService)
    {
        _clipboardService = clipboardService;
        _dispatcherService = dispatcherService;
        DataStore = dataStore;
        FilteredEntries = [];

        _updateSubscription = Observable.Interval(TimeSpan.FromMilliseconds(100))
            .Select(async _ =>
            {
                await UpdateLogCounts();
                return Task.FromResult(Unit.Default);
            })
            .Subscribe();

        if (DataStore.Entries is null) return;

        Task.Run(() => ApplyLogFiltersAsync(CancellationToken.None));

        DataStore.Entries.CollectionChanged += OnLogCollectionChanged;
    }

    private ValueTask UpdateLogCounts()
    {
        if (DataStore.Entries is null) return default;

        int errors = 0, warnings = 0, info = 0, critical = 0;

        foreach (LogModel log in DataStore.Entries)
            switch (log.LogLevel)
            {
                case LogLevel.Error: errors++; break;
                case LogLevel.Warning: warnings++; break;
                case LogLevel.Information: info++; break;
                case LogLevel.Critical: critical++; break;
                case LogLevel.Trace:
                case LogLevel.Debug:
                case LogLevel.None: break;
                default: throw new ArgumentOutOfRangeException();
            }

        _dispatcherService.InvokeOnUIAsync(() =>
        {
            ErrorsCount = errors;
            WarningsCount = warnings;
            MessagesCount = info;
            CriticalCount = critical;
        });

        return new ValueTask(Task.CompletedTask);
    }

    private void OnLogCollectionChanged(in NotifyCollectionChangedEventArgs<LogModel> e)
    {
        if (e.Action is NotifyCollectionChangedAction.Add)
        {
            if (SelectedLogLevels.Count is 0 || e.NewItems.ToArray().Any(log => SelectedLogLevels.Contains(log.LogLevel)))
                QueueFilterUpdate();
        }
        else
        {
            QueueFilterUpdate();
        }
    }

    private void QueueFilterUpdate()
    {
        lock (_syncLock)
        {
            if (_isFilteringInProgress)
            {
                _filterCancellationSource?.Cancel();
                _filterCancellationSource = new CancellationTokenSource();
            }
            else
            {
                _filterCancellationSource = new CancellationTokenSource();
                _isFilteringInProgress = true;

                Task.Run(() => ApplyLogFiltersAsync(_filterCancellationSource.Token));
            }
        }
    }

    private async Task ApplyLogFiltersAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (DataStore.Entries is null) return;

            List<LogModel> currentEntriesSnapshot;
            lock (_syncLock)
            { 
                 currentEntriesSnapshot = [.. DataStore.Entries];
            }

            var selectedLevels = SelectedLogLevels.ToHashSet();
            List<LogModel> filtered;

            if (selectedLevels.Count > 0)
                filtered = currentEntriesSnapshot.AsValueEnumerable()
                    .Where(log => selectedLevels.Contains(log.LogLevel))
                    .TakeLast(_maxLogEntries)
                    .ToList();
            else
                filtered = currentEntriesSnapshot.AsValueEnumerable()
                    .TakeLast(_maxLogEntries)
                    .ToList();

            //cancellationToken.ThrowIfCancellationRequested();

            //await _dispatcherService.InvokeOnUIAsync(() =>
            //{
            //    FilteredEntries = [.. filtered];

            //    if (AutoScroll && FilteredEntries.Count > 0)
            //    {
            //        //ScrollToItem.OnNext(FilteredEntries[^1]);
            //    }
            //}, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Operation was cancelled, do nothing
        }
        finally
        {
            lock (_syncLock)
            {
                _isFilteringInProgress = false;
            }
        }
    }

    private void CopySelectedLogAsync()
    {
        if (SelectedLogItem?.State is null) return;

        var text = $"{SelectedLogItem.LogLevel}: {SelectedLogItem.State}";
        _clipboardService.CopyToClipboard(text);
    }

    [RelayCommand]
    private void ToggleLogLevelFilter(LogLevel logLevel)
    {
        if (!SelectedLogLevels.Add(logLevel))
            SelectedLogLevels.Remove(logLevel);

        QueueFilterUpdate();
    }

    private void UpdateSelectedLogLevel(LogLevel logLevel, bool isSelected)
    {
        bool changed = isSelected
            ? SelectedLogLevels.Add(logLevel)
            : SelectedLogLevels.Remove(logLevel);

        if (changed)
            QueueFilterUpdate();
    }

    public void Dispose()
    {
        _updateSubscription?.Dispose();
        _filterCancellationSource?.Cancel();
        _filterCancellationSource?.Dispose();

        if (DataStore.Entries is not null)
            DataStore.Entries.CollectionChanged -= OnLogCollectionChanged;

        FilteredEntries?.Clear();
        SelectedLogLevels.Clear();

        GC.SuppressFinalize(this);
    }
}