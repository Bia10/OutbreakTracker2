using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Tracking;

internal static class CollectionDiffer
{
    internal static CollectionDiff<T> Diff<T>(T[] previous, T[] current)
        where T : IHasId => new CollectionDiffAccumulator<T>().Diff(previous, current);
}
