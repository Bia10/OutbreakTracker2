using ObservableCollections;
using OutbreakTracker2.Application.Views.Logging;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace OutbreakTracker2.Application.Services.LogStorage;

public class LogDataStorageService : ILogDataStorageService, IAsyncDisposable
{
    private readonly Channel<LogModel> _logChannel = Channel.CreateUnbounded<LogModel>();
    private readonly Task _processingTask;
    private readonly CancellationTokenSource _cts = new();
    private bool _isDisposed;

    public ObservableFixedSizeRingBuffer<LogModel> Entries { get; }

    public int MaxCapacity => 1000;

    public LogDataStorageService()
    {
        Entries = new ObservableFixedSizeRingBuffer<LogModel>(MaxCapacity);
        _processingTask = Task.Run(() => ProcessLogChannelAsync(_cts.Token));
    }

    public ValueTask AddEntryAsync(LogModel logModel, CancellationToken cancellationToken)
        => _logChannel.Writer.WriteAsync(logModel, cancellationToken);

    private async Task ProcessLogChannelAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (LogModel logModel in _logChannel.Reader.ReadAllAsync(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                Entries.AddLast(logModel);
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
        if (_isDisposed) return;

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