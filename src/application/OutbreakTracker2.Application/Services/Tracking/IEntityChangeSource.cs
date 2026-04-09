using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Tracking;

public interface IEntityChangeSource<T> : IDisposable
    where T : IHasId
{
    R3.Observable<T> Added { get; }
    R3.Observable<T> Removed { get; }
    R3.Observable<EntityChange<T>> Updated { get; }
    R3.Observable<CollectionDiff<T>> Diffs { get; }
}
