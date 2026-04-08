using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Tracking;

internal static class CollectionDiffer
{
    internal static CollectionDiff<T> Diff<T>(T[] previous, T[] current)
        where T : IHasId
    {
        Dictionary<Ulid, T> previousById = new(previous.Length);
        foreach (T item in previous)
            previousById[item.Id] = item;

        Dictionary<Ulid, T> currentById = new(current.Length);
        foreach (T item in current)
            currentById[item.Id] = item;

        List<T> added = [];
        List<T> removed = [];
        List<EntityChange<T>> changed = [];

        foreach (KeyValuePair<Ulid, T> pair in currentById)
        {
            if (!previousById.TryGetValue(pair.Key, out T? prev))
                added.Add(pair.Value);
            else if (!pair.Value.Equals(prev))
                changed.Add(new EntityChange<T>(prev, pair.Value));
        }

        foreach (KeyValuePair<Ulid, T> pair in previousById)
        {
            if (!currentById.ContainsKey(pair.Key))
                removed.Add(pair.Value);
        }

        return new CollectionDiff<T>(added, removed, changed);
    }
}
