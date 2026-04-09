using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Services.Tracking;

public interface IEntityTracker<T>
    where T : IHasId
{
    IEntityChangeSource<T> Changes { get; }
    Observable<AlertNotification> Alerts { get; }

    void AddRule(AlertRule<T> rule);

    /// <summary>
    /// Registers a rule that is evaluated when an entity is removed from the collection.
    /// Both <c>current</c> and <c>previous</c> in <see cref="AlertRule{T}.ShouldTrigger"/> receive
    /// the removed entity's last-known snapshot.
    /// </summary>
    void AddRemovedRule(AlertRule<T> rule);
}
