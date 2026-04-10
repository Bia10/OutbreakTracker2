using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Tracking;

public interface IAlertRule<T>
    where T : IHasId
{
    bool ShouldTrigger(T current, T? previous);
    AlertNotification CreateNotification(T current);
}
