using ObservableCollections;
using OutbreakTracker2.App.Views.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace OutbreakTracker2.App.Services.LogStorage;

public interface ILogDataStorageService
{
    ObservableList<LogModel>? Entries { get; set; }

    Task AddEntryAsync(LogModel logModel, CancellationToken cancellationToken);
}
