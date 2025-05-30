using System;
using System.Collections.Generic;
using System.Linq;

namespace OutbreakTracker2.App.Comparers;

internal sealed class ArraySequenceComparer<T> : IEqualityComparer<T[]?> where T : IEquatable<T>
{
    public bool Equals(T[]? x, T[]? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        return x.SequenceEqual(y);
    }

    public int GetHashCode(T[]? obj)
    {
        return obj is null
            ? 0
            : obj.Aggregate(obj.Length, (current, item) => HashCode.Combine(current, item.GetHashCode()));
    }
}