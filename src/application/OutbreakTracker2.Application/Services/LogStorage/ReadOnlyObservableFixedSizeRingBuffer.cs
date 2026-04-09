using System.Collections;
using ObservableCollections;

namespace OutbreakTracker2.Application.Services.LogStorage;

internal sealed class ReadOnlyObservableFixedSizeRingBuffer<T>(ObservableFixedSizeRingBuffer<T> buffer)
    : IReadOnlyObservableList<T>
{
    private readonly ObservableFixedSizeRingBuffer<T> _buffer = buffer;

    public int Count => _buffer.Count;

    public T this[int index] => _buffer[index];

    public object SyncRoot => _buffer.SyncRoot;

    public event NotifyCollectionChangedEventHandler<T>? CollectionChanged
    {
        add => _buffer.CollectionChanged += value;
        remove => _buffer.CollectionChanged -= value;
    }

    public ISynchronizedView<T, TView> CreateView<TView>(Func<T, TView> transform) => _buffer.CreateView(transform);

    public IEnumerator<T> GetEnumerator() => _buffer.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
