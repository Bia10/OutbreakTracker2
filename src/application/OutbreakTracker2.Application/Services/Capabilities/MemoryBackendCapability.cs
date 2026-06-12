using MemoryWatcher;
using OutbreakTracker2.MemoryWatcherIntegration;

namespace OutbreakTracker2.Application.Services.Capabilities;

public sealed record MemoryBackendCapability
{
    public required MemoryBackendMode Mode { get; init; }

    public required MemoryCapabilitySupportLevel Support { get; init; }

    public required MemoryObservationInvasiveness Invasiveness { get; init; }

    public required MemoryObservationPrecisionClass PrecisionClass { get; init; }

    public required MemoryObservationLatencyClass LatencyClass { get; init; }

    public bool IsConfiguredDefault { get; init; }

    public string? Reason { get; init; }
}
