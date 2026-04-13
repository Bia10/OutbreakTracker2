using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Tracking;

public sealed class PredicateRemovedAlertRule<T> : IRemovedAlertRule<T>
    where T : IHasId
{
    private readonly Func<T, bool> _condition;
    private readonly Func<T, AlertNotification> _factory;

    public PredicateRemovedAlertRule(Func<T, bool> condition, Func<T, AlertNotification> factory)
    {
        _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public bool ShouldTrigger(T removed) => _condition(removed);

    public AlertNotification CreateNotification(T removed) => _factory(removed);
}
