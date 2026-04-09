using System.Diagnostics;
using System.Threading.Channels;
using Bia.LogViewer.Core;
using ObservableCollections;
using OutbreakTracker2.Application.Services.Dispatcher;

namespace OutbreakTracker2.Application.Services.LogStorage;

public sealed class LogDataStorageService : ILogDataStorageService, IAsyncDisposable
{
    private readonly IDispatcherService _dispatcher;
    private readonly Channel<LogModel> _logChannel = Channel.CreateUnbounded<LogModel>();
    private readonly Task _processingTask;
    private readonly CancellationTokenSource _cts = new();
    private volatile bool _isDisposed;

    private readonly ObservableList<LogModel> _entries = new();

    public IReadOnlyObservableList<LogModel> Entries => _entries;

    IReadOnlyObservableList<LogModel>? ILogEntrySource.Entries => _entries;

    public int MaxCapacity => 1000;

    public LogDataStorageService(IDispatcherService dispatcher)
    {
        _dispatcher = dispatcher;
        _processingTask = Task.Run(() => ProcessLogChannelAsync(_cts.Token), _cts.Token);
    }

    public ValueTask AddEntryAsync(LogModel logModel, CancellationToken cancellationToken)
    {
        if (_isDisposed)
            return ValueTask.CompletedTask;
        return _logChannel.Writer.WriteAsync(logModel, cancellationToken);
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
                _dispatcher.PostOnUI(() =>
                {
                    if (_entries.Count >= MaxCapacity)
                        _entries.RemoveAt(0);
                    _entries.Add(logModel);
                });
            }
        }
        catch (OperationCanceledException)
        {
            Trace.TraceInformation("LogDataStorageService background task was canceled.");
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Error in LogDataStorageService background task: {ex}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
            return;

        await _cts.CancelAsync().ConfigureAwait(false);

        _logChannel.Writer.Complete();

        try
        {
            await _processingTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Trace.TraceInformation("Log processing task was canceled during DisposeAsync.");
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Error awaiting log processing task completion during DisposeAsync: {ex}");
        }
        finally
        {
            _cts.Dispose();
            _isDisposed = true;
        }
    }
}
