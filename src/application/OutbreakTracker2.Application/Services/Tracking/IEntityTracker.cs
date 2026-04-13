using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Tracking;

public interface IEntityTracker<T> : IReadOnlyEntityTracker<T>
    where T : IHasId
{
    IEntityChangeSource<T> Changes { get; }

    /// <summary>
    /// Registers a rule that is evaluated when an existing entity changes.
    /// <c>previous</c> in <see cref="IUpdatedAlertRule{T}.ShouldTrigger"/> is the last-known snapshot.
    /// </summary>
    void AddRule(IUpdatedAlertRule<T> rule);

    /// <summary>
    /// Registers a rule that is evaluated when a new entity is added to the collection.
    /// </summary>
    void AddAddedRule(IAddedAlertRule<T> rule);

    /// <summary>
    /// Registers a rule that is evaluated when an entity is removed from the collection.
    /// </summary>
    void AddRemovedRule(IRemovedAlertRule<T> rule);
}
