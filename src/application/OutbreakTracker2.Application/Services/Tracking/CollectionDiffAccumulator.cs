using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Tracking;

internal sealed class CollectionDiffAccumulator<T>
    where T : IHasId
{
    private readonly Dictionary<Ulid, T> _previousById = [];
    private readonly Dictionary<Ulid, T> _currentById = [];

    public CollectionDiff<T> Diff(T[] previous, T[] current)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(current);

        _previousById.Clear();
        _previousById.EnsureCapacity(previous.Length);
        foreach (T item in previous)
            _previousById[item.Id] = item;

        _currentById.Clear();
        _currentById.EnsureCapacity(current.Length);
        foreach (T item in current)
            _currentById[item.Id] = item;

        List<T>? added = null;
        List<T>? removed = null;
        List<EntityChange<T>>? changed = null;

        foreach (KeyValuePair<Ulid, T> pair in _currentById)
        {
            if (!_previousById.TryGetValue(pair.Key, out T? prev))
            {
                added ??= [];
                added.Add(pair.Value);
            }
            else if (!pair.Value.Equals(prev))
            {
                changed ??= [];
                changed.Add(new EntityChange<T>(prev, pair.Value));
            }
        }

        foreach (KeyValuePair<Ulid, T> pair in _previousById)
        {
            if (!_currentById.ContainsKey(pair.Key))
            {
                removed ??= [];
                removed.Add(pair.Value);
            }
        }

        return new CollectionDiff<T>(
            added is not null ? added : Array.Empty<T>(),
            removed is not null ? removed : Array.Empty<T>(),
            changed is not null ? changed : Array.Empty<EntityChange<T>>()
        );
    }
}
