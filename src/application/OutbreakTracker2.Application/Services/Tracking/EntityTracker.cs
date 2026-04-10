using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Services.Tracking;

public sealed class EntityTracker<T> : IEntityTracker<T>, IDisposable
    where T : IHasId
{
    private readonly List<IAlertRule<T>> _rules = [];
    private readonly List<IAlertRule<T>> _addedRules = [];
    private readonly List<IAlertRule<T>> _removedRules = [];
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
            foreach (T added in diff.Added)
            foreach (IAlertRule<T> rule in _addedRules)
                if (rule.ShouldTrigger(added, default))
                    _alertSubject.OnNext(rule.CreateNotification(added));

            foreach (EntityChange<T> change in diff.Changed)
            foreach (IAlertRule<T> rule in _rules)
                if (rule.ShouldTrigger(change.Current, change.Previous))
                    _alertSubject.OnNext(rule.CreateNotification(change.Current));

            // Removed rules receive (current: snapshot, previous: snapshot) — both the same
            // last-known value so rules can inspect the entity state at the moment of removal.
            foreach (T removed in diff.Removed)
            foreach (IAlertRule<T> rule in _removedRules)
                if (rule.ShouldTrigger(removed, removed))
                    _alertSubject.OnNext(rule.CreateNotification(removed));
        });
    }

    public void AddRule(IAlertRule<T> rule) => _rules.Add(rule ?? throw new ArgumentNullException(nameof(rule)));

    public void AddAddedRule(IAlertRule<T> rule) =>
        _addedRules.Add(rule ?? throw new ArgumentNullException(nameof(rule)));

    public void AddRemovedRule(IAlertRule<T> rule) =>
        _removedRules.Add(rule ?? throw new ArgumentNullException(nameof(rule)));

    public void Dispose()
    {
        _subscription.Dispose();
        _alertSubject.Dispose();
        Changes.Dispose();
    }
}
