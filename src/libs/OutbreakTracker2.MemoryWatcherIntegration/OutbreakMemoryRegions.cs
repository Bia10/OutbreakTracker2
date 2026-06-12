namespace OutbreakTracker2.MemoryWatcherIntegration;

public readonly record struct OutbreakTrackerMemoryRegionDefinition(
    string Name,
    nint BaseAddress,
    nuint ByteLength,
    OutbreakTrackerMemoryDomains Domains
);

public interface IOutbreakTrackerMemoryRegionCatalog
{
    IReadOnlyList<OutbreakTrackerMemoryRegionDefinition> CreateRegions(nint eememBaseAddress);
}
