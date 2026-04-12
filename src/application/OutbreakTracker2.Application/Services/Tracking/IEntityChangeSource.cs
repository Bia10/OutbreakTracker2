using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Tracking;

/// <summary>
/// A multicast observable that emits <see cref="CollectionDiff{T}"/> values whenever the
/// entity snapshot changes, along with convenience projections for added, removed, and
/// updated entities.
/// </summary>
public interface IEntityChangeSource<T> : IDisposable
    where T : IHasId
{
    /// <summary>Emits each entity that appeared in the latest snapshot but was absent from the previous one.</summary>
    R3.Observable<T> Added { get; }

    /// <summary>Emits each entity that was present in the previous snapshot but absent from the latest one.</summary>
    R3.Observable<T> Removed { get; }

    /// <summary>Emits each entity whose properties changed between the previous and latest snapshots.</summary>
    R3.Observable<EntityChange<T>> Updated { get; }

    /// <summary>Emits the full diff for every polling cycle, containing all added, removed, and changed entities.</summary>
    R3.Observable<CollectionDiff<T>> Diffs { get; }
}
