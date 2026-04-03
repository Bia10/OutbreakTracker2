using Bia.LogViewer.Core;
using ObservableCollections;

namespace OutbreakTracker2.Application.Services.LogStorage;

public interface ILogDataStorageService : ILogEntrySource
{
    new ObservableList<LogModel> Entries { get; }

    ValueTask AddEntryAsync(LogModel logModel, CancellationToken cancellationToken);

    int MaxCapacity { get; }
}
