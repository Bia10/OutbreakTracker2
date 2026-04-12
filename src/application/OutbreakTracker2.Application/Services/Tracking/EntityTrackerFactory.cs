using Microsoft.Extensions.Logging;
using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Services.Tracking;

public sealed class EntityTrackerFactory(ILoggerFactory loggerFactory) : IEntityTrackerFactory
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory;

    public IEntityTracker<T> Create<T>(Observable<T[]> snapshots)
        where T : IHasId
    {
        ArgumentNullException.ThrowIfNull(snapshots);

        return new EntityTracker<T>(
            new EntityChangeSource<T>(snapshots),
            _loggerFactory.CreateLogger<EntityTracker<T>>()
        );
    }
}
