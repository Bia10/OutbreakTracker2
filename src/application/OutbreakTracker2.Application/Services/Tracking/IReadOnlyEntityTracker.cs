using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Services.Tracking;

public interface IReadOnlyEntityTracker<T> : IDisposable
    where T : IHasId
{
    IEntityChangeSource<T> Changes { get; }
    Observable<AlertNotification> Alerts { get; }
}
