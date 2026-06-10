namespace OutbreakTracker2.MemoryWatcherIntegration;

public readonly record struct OutbreakTrackerMemoryRegionDefinition(string Name, nint BaseAddress, nuint ByteLength);

public interface IOutbreakTrackerMemoryRegionCatalog
{
    IReadOnlyList<OutbreakTrackerMemoryRegionDefinition> CreateRegions(nint eememBaseAddress);
}
