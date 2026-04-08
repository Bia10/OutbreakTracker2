using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Tracking;

public abstract class AlertRule<T>
    where T : IHasId
{
    public abstract bool ShouldTrigger(T current, T? previous);
    public abstract AlertNotification CreateNotification(T current);
}
