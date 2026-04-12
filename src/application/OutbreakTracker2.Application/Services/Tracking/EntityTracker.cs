using Microsoft.Extensions.Logging;
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
    private readonly ILogger<EntityTracker<T>> _logger;
    private readonly IDisposable _subscription;

    public IEntityChangeSource<T> Changes { get; }
    public Observable<AlertNotification> Alerts { get; }

    public EntityTracker(IEntityChangeSource<T> changes, ILogger<EntityTracker<T>> logger)
    {
        Changes = changes;
        _logger = logger;
        Alerts = _alertSubject;

        // Subscribe to Diffs — one callback per polling cycle processes all changed and
        // removed entities in a single pass rather than one callback per entity.
        _subscription = changes.Diffs.Subscribe(diff =>
        {
            foreach (T added in diff.Added)
            foreach (IAlertRule<T> rule in _addedRules)
                EvaluateRule(rule, added, default);

            foreach (EntityChange<T> change in diff.Changed)
            foreach (IAlertRule<T> rule in _rules)
                EvaluateRule(rule, change.Current, change.Previous);

            // Removed rules receive (current: snapshot, previous: snapshot) — both the same
            // last-known value so rules can inspect the entity state at the moment of removal.
            foreach (T removed in diff.Removed)
            foreach (IAlertRule<T> rule in _removedRules)
                EvaluateRule(rule, removed, removed);
        });
    }

    private void EvaluateRule(IAlertRule<T> rule, T current, T? previous)
    {
        try
        {
            if (rule.ShouldTrigger(current, previous))
                _alertSubject.OnNext(rule.CreateNotification(current));
        }
        catch (Exception ex)
        {
            // Log and skip — one faulting rule must not terminate the subscription.
            _logger.LogError(ex, "Alert rule {Rule} threw during evaluation; rule skipped", rule.GetType().Name);
        }
    }

    public void AddRule(IAlertRule<T> rule) => _rules.Add(rule ?? throw new ArgumentNullException(nameof(rule)));

    public void AddAddedRule(IAlertRule<T> rule) =>
        _addedRules.Add(rule ?? throw new ArgumentNullException(nameof(rule)));

    public void AddRemovedRule(IAlertRule<T> rule) =>
        _removedRules.Add(rule ?? throw new ArgumentNullException(nameof(rule)));

    public void Dispose()
    {
        // Cancel the upstream producer before disposing the subject to prevent
        // a final OnNext from reaching an already-disposed subject.
        _subscription.Dispose();
        _alertSubject.Dispose();
        Changes.Dispose();
    }
}
