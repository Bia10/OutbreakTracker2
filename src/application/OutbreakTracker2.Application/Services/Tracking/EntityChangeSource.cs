using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Services.Tracking;

public sealed class EntityChangeSource<T> : IEntityChangeSource<T>, IDisposable
    where T : IHasId
{
    private readonly IDisposable _connection;

    public Observable<T> Added { get; }
    public Observable<T> Removed { get; }
    public Observable<EntityChange<T>> Updated { get; }
    public Observable<CollectionDiff<T>> Diffs { get; }

    public EntityChangeSource(Observable<T[]> snapshots)
    {
        ConnectableObservable<CollectionDiff<T>> published = snapshots
            .Scan(seed: (Prev: Array.Empty<T>(), Curr: Array.Empty<T>()), (acc, next) => (acc.Curr, next))
            .Select(pair => CollectionDiffer.Diff(pair.Prev, pair.Curr))
            .Publish();

        Diffs = published.AsObservable();
        Added = Diffs.SelectMany(d => d.Added.ToObservable());
        Removed = Diffs.SelectMany(d => d.Removed.ToObservable());
        Updated = Diffs.SelectMany(d => d.Changed.ToObservable());

        _connection = published.Connect();
    }

    public void Dispose() => _connection.Dispose();
}
