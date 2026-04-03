using System.Threading.Channels;
using Bia.LogViewer.Core;
using ObservableCollections;
using OutbreakTracker2.Application.Services.Dispatcher;

namespace OutbreakTracker2.Application.Services.LogStorage;

public class LogDataStorageService : ILogDataStorageService, IAsyncDisposable
{
    private readonly IDispatcherService _dispatcher;
    private readonly Channel<LogModel> _logChannel = Channel.CreateUnbounded<LogModel>();
    private readonly Task _processingTask;
    private readonly CancellationTokenSource _cts = new();
    private bool _isDisposed;

    public ObservableList<LogModel> Entries { get; } = [];

    IReadOnlyObservableList<LogModel>? ILogEntrySource.Entries => Entries;

    public int MaxCapacity => 1000;

    public LogDataStorageService(IDispatcherService dispatcher)
    {
        _dispatcher = dispatcher;
        _processingTask = Task.Run(() => ProcessLogChannelAsync(_cts.Token), _cts.Token);
    }

    public ValueTask AddEntryAsync(LogModel logModel, CancellationToken cancellationToken) =>
        _logChannel.Writer.WriteAsync(logModel, cancellationToken);

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
                    if (Entries.Count >= MaxCapacity)
                        Entries.RemoveAt(0);
                    Entries.Add(logModel);
                });
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("LogDataStorageService background task was canceled.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in LogDataStorageService background task: {ex}");
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
            Console.WriteLine("Log processing task was canceled during DisposeAsync.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error awaiting log processing task completion during DisposeAsync: {ex}");
        }
        finally
        {
            _cts.Dispose();
            _isDisposed = true;
        }
    }
}
