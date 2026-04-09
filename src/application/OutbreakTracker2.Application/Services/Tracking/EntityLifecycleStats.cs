using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Services.Tracking;

public sealed class EntityLifecycleStats<T> : IEntityLifecycleStats<T>, IDisposable
    where T : IHasId
{
    private readonly ReactiveProperty<long> _addedCount = new(0L);
    private readonly ReactiveProperty<long> _removedCount = new(0L);
    private readonly ReactiveProperty<DateTimeOffset?> _lastEventAt = new(null);
    private readonly IDisposable _subscription;

    public Observable<long> AddedCount => _addedCount;
    public Observable<long> RemovedCount => _removedCount;
    public Observable<DateTimeOffset?> LastEventAt => _lastEventAt;

    public EntityLifecycleStats(IEntityChangeSource<T> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);

        _subscription = changes.Diffs.Subscribe(diff =>
        {
            bool anyEvent = false;

            if (diff.Added.Count > 0)
            {
                _addedCount.Value += diff.Added.Count;
                anyEvent = true;
            }

            if (diff.Removed.Count > 0)
            {
                _removedCount.Value += diff.Removed.Count;
                anyEvent = true;
            }

            if (anyEvent)
                _lastEventAt.Value = DateTimeOffset.UtcNow;
        });
    }

    public void Dispose()
    {
        _subscription.Dispose();
        _addedCount.Dispose();
        _removedCount.Dispose();
        _lastEventAt.Dispose();
    }
}
