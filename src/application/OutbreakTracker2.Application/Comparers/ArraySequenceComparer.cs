namespace OutbreakTracker2.Application.Comparers;

internal sealed class ArraySequenceComparer<T> : IEqualityComparer<T[]?>
    where T : IEquatable<T>
{
    public bool Equals(T[]? x, T[]? y)
    {
        if (ReferenceEquals(x, y))
            return true;
        if (x is null || y is null)
            return false;

        return x.AsSpan().SequenceEqual(y.AsSpan());
    }

    public int GetHashCode(T[]? obj)
    {
        if (obj is null)
            return 0;

        // Seed with both the element type and the length so same-length arrays
        // of different element types never share a starting hash state.
        int seed = HashCode.Combine(typeof(T).GetHashCode(), obj.Length);
        return obj.Aggregate(seed, (current, item) => HashCode.Combine(current, item.GetHashCode()));
    }
}
