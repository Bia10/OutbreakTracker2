using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Services.Tracking;

/// <summary>
/// Provides lifecycle statistics for a tracked entity type: spawn/join count,
/// removal/death count, and the timestamp of the most recent event.
/// </summary>
public interface IEntityLifecycleStats<T>
    where T : IHasId
{
    /// <summary>Total number of entities added to the collection since tracking started.</summary>
    Observable<long> AddedCount { get; }

    /// <summary>Total number of entities removed from the collection since tracking started.</summary>
    Observable<long> RemovedCount { get; }

    /// <summary>Timestamp of the most recent add or remove event. Null until the first event.</summary>
    Observable<DateTimeOffset?> LastEventAt { get; }
}
