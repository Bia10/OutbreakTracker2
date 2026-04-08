using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Tracking;

public sealed record CollectionDiff<T>(
    IReadOnlyList<T> Added,
    IReadOnlyList<T> Removed,
    IReadOnlyList<EntityChange<T>> Changed
)
    where T : IHasId;
