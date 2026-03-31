using ObservableCollections;
using OutbreakTracker2.Application.Views.Logging;

namespace OutbreakTracker2.Application.Services.LogStorage;

public interface ILogDataStorageService
{
    ObservableFixedSizeRingBuffer<LogModel> Entries { get; }

    ValueTask AddEntryAsync(LogModel logModel, CancellationToken cancellationToken);

    int MaxCapacity { get; }
}
