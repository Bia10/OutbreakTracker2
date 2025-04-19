using ObservableCollections;
using OutbreakTracker2.App.Views.Logging;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace OutbreakTracker2.App.Services;

public class LogDataStorageService : ILogDataStorageService
{
    private readonly IDispatcherService _dispatcherService;
    private readonly Channel<LogModel> _logChannel = Channel.CreateUnbounded<LogModel>();

    public LogDataStorageService(IDispatcherService dispatcherService)
    {
        _dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
        Entries = [];

        _ = ProcessLogChannelAsync(CancellationToken.None);
    }

    public ObservableList<LogModel>? Entries { get; set; }

    public async Task AddEntryAsync(LogModel logModel, CancellationToken cancellationToken)
    {
        await _logChannel.Writer.WriteAsync(logModel, cancellationToken).ConfigureAwait(false);
    }

    private async Task ProcessLogChannelAsync(CancellationToken cancellationToken)
    {
        await foreach (LogModel logModel in _logChannel.Reader.ReadAllAsync(cancellationToken))
            await _dispatcherService.InvokeOnUIAsync(() =>
            {
                Entries?.Add(logModel);
            }, cancellationToken).ConfigureAwait(false);
    }
}
