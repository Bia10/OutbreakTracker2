using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Tracking;

public interface IRemovedAlertRule<T>
    where T : IHasId
{
    bool ShouldTrigger(T removed);
    AlertNotification CreateNotification(T removed);
}
