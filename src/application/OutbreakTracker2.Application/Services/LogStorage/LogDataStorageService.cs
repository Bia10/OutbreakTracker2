using System.Threading.Channels;
using Bia.LogViewer.Core;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using OutbreakTracker2.Application.Services.Dispatcher;

namespace OutbreakTracker2.Application.Services.LogStorage;

public sealed class LogDataStorageService : ILogDataStorageService, IAsyncDisposable
{
    private readonly ILogger<LogDataStorageService> _logger;
    private readonly IDispatcherService _dispatcher;
    private readonly Channel<LogModel> _logChannel = Channel.CreateBounded<LogModel>(
        new BoundedChannelOptions(capacity: 2000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleWriter = false,
            SingleReader = true,
        }
    );
    private readonly Task _processingTask;
    private readonly CancellationTokenSource _cts = new();
    private volatile bool _isDisposed;

    private readonly ObservableFixedSizeRingBuffer<LogModel> _entries = new(1000);
    private readonly ReadOnlyObservableFixedSizeRingBuffer<LogModel> _entriesView;

    public IReadOnlyObservableList<LogModel> Entries => _entriesView;

    IReadOnlyObservableList<LogModel>? ILogEntrySource.Entries => _entriesView;

    public int MaxCapacity => 1000;

    public LogDataStorageService(ILogger<LogDataStorageService> logger, IDispatcherService dispatcher)
    {
        _logger = logger;
        _dispatcher = dispatcher;
        _entriesView = new(_entries);
        _processingTask = Task.Run(() => ProcessLogChannelAsync(_cts.Token), _cts.Token);
    }

    public async ValueTask AddEntryAsync(LogModel logModel, CancellationToken cancellationToken)
    {
        if (_isDisposed)
            return;

        try
        {
            await _logChannel.Writer.WriteAsync(logModel, cancellationToken).ConfigureAwait(false);
        }
        catch (ChannelClosedException)
        {
            _logger.LogDebug("Skipping log entry because the storage channel was closed during shutdown.");
        }
    }

    private async Task ProcessLogChannelAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (
                LogModel logModel in _logChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false)
            )
            {
                cancellationToken.ThrowIfCancellationRequested();
                _dispatcher.PostOnUI(() => _entries.AddLast(logModel));
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("LogDataStorageService background task was canceled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in LogDataStorageService background task");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
            return;

        await _cts.CancelAsync().ConfigureAwait(false);

        _logChannel.Writer.TryComplete();

        try
        {
            await _processingTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Log processing task was canceled during DisposeAsync.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error awaiting log processing task completion during DisposeAsync");
        }
        finally
        {
            _cts.Dispose();
            _isDisposed = true;
        }
    }
}
