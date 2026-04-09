using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Services.Tracking;

public sealed class EntityTracker<T> : IEntityTracker<T>, IDisposable
    where T : IHasId
{
    private readonly List<AlertRule<T>> _rules = [];
    private readonly List<AlertRule<T>> _removedRules = [];
    private readonly Subject<AlertNotification> _alertSubject = new();
    private readonly IDisposable _subscription;

    public IEntityChangeSource<T> Changes { get; }
    public Observable<AlertNotification> Alerts { get; }

    public EntityTracker(IEntityChangeSource<T> changes)
    {
        Changes = changes;
        Alerts = _alertSubject;

        // Subscribe to Diffs — one callback per polling cycle processes all changed and
        // removed entities in a single pass rather than one callback per entity.
        _subscription = changes.Diffs.Subscribe(diff =>
        {
            foreach (EntityChange<T> change in diff.Changed)
            foreach (AlertRule<T> rule in _rules)
                if (rule.ShouldTrigger(change.Current, change.Previous))
                    _alertSubject.OnNext(rule.CreateNotification(change.Current));

            // Removed rules receive (current: snapshot, previous: snapshot) — both the same
            // last-known value so rules can inspect the entity state at the moment of removal.
            foreach (T removed in diff.Removed)
            foreach (AlertRule<T> rule in _removedRules)
                if (rule.ShouldTrigger(removed, removed))
                    _alertSubject.OnNext(rule.CreateNotification(removed));
        });
    }

    public void AddRule(AlertRule<T> rule) => _rules.Add(rule ?? throw new ArgumentNullException(nameof(rule)));

    public void AddRemovedRule(AlertRule<T> rule) =>
        _removedRules.Add(rule ?? throw new ArgumentNullException(nameof(rule)));

    public void Dispose()
    {
        _subscription.Dispose();
        _alertSubject.Dispose();
        (Changes as IDisposable)?.Dispose();
    }
}
