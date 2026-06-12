using System.Text.Json.Serialization;
using MemoryWatcher;
using MemoryWatcher.Remote;

namespace OutbreakTracker2.MemoryWatcherIntegration;

[JsonConverter(typeof(JsonStringEnumConverter<MemoryBackendMode>))]
public enum MemoryBackendMode
{
    Legacy = 0,
    MemoryWatcher = 1,
}

public sealed record MemoryWatcherSettings
{
    public MemoryBackendMode Backend { get; init; } = MemoryBackendMode.MemoryWatcher;

    [JsonConverter(typeof(JsonStringEnumConverter<WatchBackendKind>))]
    public WatchBackendKind PreferredBackend { get; init; } = WatchBackendKind.Auto;

    [JsonConverter(typeof(JsonStringEnumConverter<WatchPrecision>))]
    public WatchPrecision PreferredPrecision { get; init; } = WatchPrecision.SnapshotBitExact;

    public bool AllowFallback { get; init; } = true;

    public string? NativeLibraryPath { get; init; }

    public bool AllowIntrusiveBackends { get; init; }

    public int EventBufferCapacity { get; init; } = 256;

    public int HashBlockSizeBytes { get; init; } = 64;

    public bool UseHashIndex { get; init; } = true;

    public MemoryWatchSessionOptions ToSessionOptions(IReadOnlyList<int>? hardwareThreadIds = null) =>
        new()
        {
            PreferredBackend = PreferredBackend,
            PreferredPrecision = PreferredPrecision,
            AllowFallback = AllowFallback,
            AllowIntrusiveBackends = AllowIntrusiveBackends,
            AllowNativeAgent = PreferredBackend == WatchBackendKind.NativeAgent,
            HardwareThreadIds = hardwareThreadIds ?? Array.Empty<int>(),
            EventBufferCapacity = EventBufferCapacity,
            HashBlockSizeBytes = HashBlockSizeBytes,
            UseHashIndex = UseHashIndex,
        };
}
