using ObservableCollections;
using OutbreakTracker2.App.Views.Logging;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace OutbreakTracker2.App.Services.LogStorage;

public class LogDataStorageService : ILogDataStorageService, IDisposable
{
    private readonly Channel<LogModel> _logChannel = Channel.CreateUnbounded<LogModel>();
    private readonly Task _processingTask;
    private readonly CancellationTokenSource _cts = new();

    public ObservableFixedSizeRingBuffer<LogModel> Entries { get; private set; }

    public int MaxCapacity { get; } = 1000;

    public LogDataStorageService()
    {
        Entries = new ObservableFixedSizeRingBuffer<LogModel>(MaxCapacity);

        _processingTask = Task.Run(() => ProcessLogChannelAsync(_cts.Token));
    }

    /// <summary>
    /// Adds a log entry to the internal channel for asynchronous processing.
    /// </summary>
    /// <returns>A ValueTask representing the asynchronous write operation.</returns>
    public async ValueTask AddEntryAsync(LogModel logModel, CancellationToken cancellationToken)
    {
        await _logChannel.Writer.WriteAsync(logModel, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Background task that reads log entries from the channel and adds them to the Entries ring buffer.
    /// This method runs entirely on a background thread, and modifications to Entries
    /// do NOT need to be marshaled to the UI thread.
    /// </summary>
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

    /// <summary>
    /// Disposes the LogDataStorageService and its resources.
    /// </summary>
    public async void Dispose()
    {
        await _cts.CancelAsync().ConfigureAwait(false);

        _logChannel.Writer.Complete();

        try
        {
            await _processingTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Log processing task was canceled.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error awaiting log processing task completion: {ex}");
        }
        finally
        {
            _cts.Dispose();
        }
    }
}