using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Services.Tracking;

public interface IEntityTracker<T>
    where T : IHasId
{
    IEntityChangeSource<T> Changes { get; }
    Observable<AlertNotification> Alerts { get; }

    void AddRule(AlertRule<T> rule);
}
