using MemoryWatcher.Remote;

namespace OutbreakTracker2.Application.Services.Capabilities;

public sealed record MemoryBackendCapabilityReport
{
    public required MemoryWatchHostEnvironment Host { get; init; }

    public required MemoryWatchCapabilityNegotiationResult MemoryWatcher { get; init; }

    public required IReadOnlyList<MemoryBackendCapability> Backends { get; init; }
}
