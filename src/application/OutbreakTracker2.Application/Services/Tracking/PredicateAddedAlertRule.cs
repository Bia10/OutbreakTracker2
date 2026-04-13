using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Tracking;

public sealed class PredicateAddedAlertRule<T> : IAddedAlertRule<T>
    where T : IHasId
{
    private readonly Func<T, bool> _condition;
    private readonly Func<T, AlertNotification> _factory;

    public PredicateAddedAlertRule(Func<T, bool> condition, Func<T, AlertNotification> factory)
    {
        _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public bool ShouldTrigger(T current) => _condition(current);

    public AlertNotification CreateNotification(T current) => _factory(current);
}
