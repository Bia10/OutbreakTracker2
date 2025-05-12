using ObservableCollections;

namespace OutbreakTracker2.Extensions;

public static class ObservableListExtensions
{
    /// <summary>
    /// Replaces all existing items in the ObservableList with the provided new items from an IEnumerable.
    /// This operation first clears the list and then adds the new items.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The ObservableList<T> instance to extend.</param>
    /// <param name="newItems">The enumerable collection of items to add.</param>
    public static void ReplaceAll<T>(this ObservableList<T> list, IEnumerable<T> newItems)
    {
        list.Clear();
        list.AddRange(newItems);
    }

    /// <summary>
    /// Replaces all existing items in the ObservableList with the provided new items from a ReadOnlySpan.
    /// This operation first clears the list and then adds the new items.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The ObservableList<T> instance to extend.</param>
    /// <param name="newItems">The ReadOnlySpan<T> of items to add.</param>
    public static void ReplaceAll<T>(this ObservableList<T> list, ReadOnlySpan<T> newItems)
    {
        list.Clear();
        list.AddRange(newItems);
    }

    /// <summary>
    /// Replaces all existing items in the ObservableList with the provided new items from a Span.
    /// This operation first clears the list and then adds the new items.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The ObservableList<T> instance to extend.</param>
    /// <param name="newItems">The Span<T> of items to add.</param>
    public static void ReplaceAll<T>(this ObservableList<T> list, Span<T> newItems)
    {
        list.Clear();
        list.AddRange(newItems);
    }
}