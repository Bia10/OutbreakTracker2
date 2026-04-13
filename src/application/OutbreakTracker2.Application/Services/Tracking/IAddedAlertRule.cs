using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Tracking;

public interface IAddedAlertRule<T>
    where T : IHasId
{
    bool ShouldTrigger(T current);
    AlertNotification CreateNotification(T current);
}
