using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Services.Tracking;

public sealed class EntityTracker<T> : IEntityTracker<T>, IDisposable
    where T : IHasId
{
    private readonly List<AlertRule<T>> _rules = [];
    private readonly Subject<AlertNotification> _alertSubject = new();
    private readonly IDisposable _subscription;

    public IEntityChangeSource<T> Changes { get; }
    public Observable<AlertNotification> Alerts { get; }

    public EntityTracker(IEntityChangeSource<T> changes)
    {
        Changes = changes;
        Alerts = _alertSubject;

        // Subscribe to Diffs — one callback per polling cycle processes all changed entities
        // in a single pass rather than one callback per changed entity.
        _subscription = changes.Diffs.Subscribe(diff =>
        {
            foreach (EntityChange<T> change in diff.Changed)
            foreach (AlertRule<T> rule in _rules)
                if (rule.ShouldTrigger(change.Current, change.Previous))
                    _alertSubject.OnNext(rule.CreateNotification(change.Current));
        });
    }

    public void AddRule(AlertRule<T> rule) => _rules.Add(rule ?? throw new ArgumentNullException(nameof(rule)));

    public void Dispose()
    {
        _subscription.Dispose();
        _alertSubject.Dispose();
        (Changes as IDisposable)?.Dispose();
    }
}
