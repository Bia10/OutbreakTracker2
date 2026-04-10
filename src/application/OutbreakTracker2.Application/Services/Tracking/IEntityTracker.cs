using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Services.Tracking;

public interface IEntityTracker<T> : IDisposable
    where T : IHasId
{
    IEntityChangeSource<T> Changes { get; }
    Observable<AlertNotification> Alerts { get; }

    void AddRule(IAlertRule<T> rule);

    /// <summary>
    /// Registers a rule that is evaluated when a new entity is added to the collection.
    /// <c>previous</c> in <see cref="IAlertRule{T}.ShouldTrigger"/> is always <c>default</c> (null).
    /// </summary>
    void AddAddedRule(IAlertRule<T> rule);

    /// <summary>
    /// Registers a rule that is evaluated when an entity is removed from the collection.
    /// Both <c>current</c> and <c>previous</c> in <see cref="IAlertRule{T}.ShouldTrigger"/> receive
    /// the removed entity's last-known snapshot.
    /// </summary>
    void AddRemovedRule(IAlertRule<T> rule);
}
