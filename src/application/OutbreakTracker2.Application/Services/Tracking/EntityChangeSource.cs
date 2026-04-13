using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Services.Tracking;

/// <summary>
/// Converts a stream of entity snapshots into a multicast diff stream.
/// </summary>
/// <remarks>
/// <para>
/// Internally uses <c>Publish().Connect()</c> rather than <c>Share()</c> so that the upstream
/// <c>Scan</c> accumulator is never reset: the connection is established once (on construction)
/// and lives until <see cref="Dispose"/> is called.  A subscriber that joins after the first
/// emissions will receive future diffs relative to the state at the time they subscribe, not
/// a cold replay from the beginning.
/// </para>
/// <para>
/// Pipeline: <c>DataManager</c> snapshot → <c>Scan</c> (produces prev/curr pair) →
/// <c>CollectionDiffer.Diff</c> → <see cref="Diffs"/> multicast → <see cref="Added"/>,
/// <see cref="Removed"/>, <see cref="Updated"/> convenience projections.
/// </para>
/// </remarks>
public sealed class EntityChangeSource<T> : IEntityChangeSource<T>, IDisposable
    where T : IHasId
{
    private readonly IDisposable _connection;

    /// <inheritdoc/>
    public Observable<T> Added { get; }

    /// <inheritdoc/>
    public Observable<T> Removed { get; }

    /// <inheritdoc/>
    public Observable<EntityChange<T>> Updated { get; }

    /// <inheritdoc/>
    public Observable<CollectionDiff<T>> Diffs { get; }

    public EntityChangeSource(Observable<T[]> snapshots)
    {
        CollectionDiffAccumulator<T> diffAccumulator = new();

        ConnectableObservable<CollectionDiff<T>> published = snapshots
            .Scan(seed: (Prev: Array.Empty<T>(), Curr: Array.Empty<T>()), (acc, next) => (acc.Curr, next))
            .Select(pair => diffAccumulator.Diff(pair.Prev, pair.Curr))
            .Publish();

        Diffs = published.AsObservable();
        Added = Diffs.SelectMany(d => d.Added.ToObservable());
        Removed = Diffs.SelectMany(d => d.Removed.ToObservable());
        Updated = Diffs.SelectMany(d => d.Changed.ToObservable());

        _connection = published.Connect();
    }

    public void Dispose() => _connection.Dispose();
}
