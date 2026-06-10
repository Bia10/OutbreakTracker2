using MemoryWatcher.Remote;

namespace OutbreakTracker2.MemoryWatcherIntegration;

public enum MemoryBackendMode
{
    Legacy = 0,
    MemoryWatcher = 1,
}

public sealed record MemoryWatcherSettings
{
    public MemoryBackendMode Backend { get; init; } = MemoryBackendMode.Legacy;

    public string? NativeLibraryPath { get; init; }

    public bool AllowIntrusiveBackends { get; init; }

    public int EventBufferCapacity { get; init; } = 256;

    public int HashBlockSizeBytes { get; init; } = 64;

    public bool UseHashIndex { get; init; } = true;

    public MemoryWatchSessionOptions ToSessionOptions() =>
        new()
        {
            AllowFallback = true,
            AllowIntrusiveBackends = AllowIntrusiveBackends,
            EventBufferCapacity = EventBufferCapacity,
            HashBlockSizeBytes = HashBlockSizeBytes,
            UseHashIndex = UseHashIndex,
        };
}
