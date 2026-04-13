using Microsoft.Extensions.Logging;
using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Services.Tracking;

public sealed class EntityTracker<T> : IEntityTracker<T>, IDisposable
    where T : IHasId
{
    private int _rulesLocked;
    private readonly List<IUpdatedAlertRule<T>> _rules = [];
    private readonly List<IAddedAlertRule<T>> _addedRules = [];
    private readonly List<IRemovedAlertRule<T>> _removedRules = [];
    private readonly Subject<AlertNotification> _alertSubject = new();
    private readonly ILogger<EntityTracker<T>> _logger;
    private readonly IDisposable _subscription;

    /// <summary>
    /// Low-level diff source owned by this tracker. Consumers may subscribe to it,
    /// but the tracker controls its lifetime and disposes it during tracker disposal.
    /// </summary>
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
            Volatile.Write(ref _rulesLocked, 1);

            foreach (T added in diff.Added)
            foreach (IAddedAlertRule<T> rule in _addedRules)
                EvaluateAddedRule(rule, added);

            foreach (EntityChange<T> change in diff.Changed)
            foreach (IUpdatedAlertRule<T> rule in _rules)
                EvaluateUpdatedRule(rule, change.Current, change.Previous);

            foreach (T removed in diff.Removed)
            foreach (IRemovedAlertRule<T> rule in _removedRules)
                EvaluateRemovedRule(rule, removed);
        });
    }

    private void EvaluateUpdatedRule(IUpdatedAlertRule<T> rule, T current, T previous)
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

    private void EvaluateAddedRule(IAddedAlertRule<T> rule, T current)
    {
        try
        {
            if (rule.ShouldTrigger(current))
                _alertSubject.OnNext(rule.CreateNotification(current));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Alert rule {Rule} threw during evaluation; rule skipped", rule.GetType().Name);
        }
    }

    private void EvaluateRemovedRule(IRemovedAlertRule<T> rule, T removed)
    {
        try
        {
            if (rule.ShouldTrigger(removed))
                _alertSubject.OnNext(rule.CreateNotification(removed));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Alert rule {Rule} threw during evaluation; rule skipped", rule.GetType().Name);
        }
    }

    private void ThrowIfRulesLocked()
    {
        if (Volatile.Read(ref _rulesLocked) != 0)
            throw new InvalidOperationException(
                "Alert rules must be registered before the tracker processes its first diff."
            );
    }

    public void AddRule(IUpdatedAlertRule<T> rule)
    {
        ThrowIfRulesLocked();
        _rules.Add(rule ?? throw new ArgumentNullException(nameof(rule)));
    }

    public void AddAddedRule(IAddedAlertRule<T> rule)
    {
        ThrowIfRulesLocked();
        _addedRules.Add(rule ?? throw new ArgumentNullException(nameof(rule)));
    }

    public void AddRemovedRule(IRemovedAlertRule<T> rule)
    {
        ThrowIfRulesLocked();
        _removedRules.Add(rule ?? throw new ArgumentNullException(nameof(rule)));
    }

    public void Dispose()
    {
        // Cancel the upstream producer before disposing the subject to prevent
        // a final OnNext from reaching an already-disposed subject.
        _subscription.Dispose();
        _alertSubject.Dispose();
        Changes.Dispose();
    }
}
