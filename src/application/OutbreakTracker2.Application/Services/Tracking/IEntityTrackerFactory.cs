using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Services.Tracking;

public interface IEntityTrackerFactory
{
    IEntityTracker<T> Create<T>(Observable<T[]> snapshots)
        where T : IHasId;
}
