using MemoryWatcher;
using MemoryWatcher.Remote;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;

namespace OutbreakTracker2.MemoryWatcherIntegration;

internal static class MemoryWatchActivityPlanFactory
{
    private const long PageSizeBytes = 4096;
    private const OutbreakTrackerMemoryDomains InGameWakeDomains =
        OutbreakTrackerMemoryDomains.Scenario
        | OutbreakTrackerMemoryDomains.InGamePlayers
        | OutbreakTrackerMemoryDomains.Enemies
        | OutbreakTrackerMemoryDomains.Doors;

    public static bool SupportsDedicatedActivity(WatchBackendKind backend) =>
        backend
            is WatchBackendKind.DirtyPage
                or WatchBackendKind.SoftDirty
                or WatchBackendKind.PageFault
                or WatchBackendKind.HardwareWatchpoint;

    public static OutbreakTrackerMemoryWatchPlanItem[] CreateReadPlan(
        IReadOnlyList<OutbreakTrackerMemoryRegionDefinition> regions
    )
    {
        ArgumentNullException.ThrowIfNull(regions);

        OutbreakTrackerMemoryWatchPlanItem[] plan = new OutbreakTrackerMemoryWatchPlanItem[regions.Count];
        for (int i = 0; i < regions.Count; i++)
        {
            OutbreakTrackerMemoryRegionDefinition region = regions[i];
            plan[i] = new OutbreakTrackerMemoryWatchPlanItem(
                region.Name,
                MemoryRegionSpec.Absolute(region.BaseAddress, region.ByteLength),
                region.Domains
            );
        }

        return plan;
    }

    public static bool TryCreateActivityPlan(
        WatchBackendKind backend,
        IReadOnlyList<OutbreakTrackerMemoryRegionDefinition> regions,
        out OutbreakTrackerMemoryWatchPlanItem[] plan
    ) => TryCreateActivityPlan(backend, nint.Zero, GameFile.Unknown, regions, out plan);

    public static bool TryCreateActivityPlan(
        WatchBackendKind backend,
        nint eememBaseAddress,
        GameFile activeGameFile,
        IReadOnlyList<OutbreakTrackerMemoryRegionDefinition> regions,
        out OutbreakTrackerMemoryWatchPlanItem[] plan
    )
    {
        ArgumentNullException.ThrowIfNull(regions);

        if (!SupportsDedicatedActivity(backend) || regions.Count == 0)
        {
            plan = [];
            return false;
        }

        if (backend == WatchBackendKind.HardwareWatchpoint)
        {
            return TryCreateHardwareWatchpointPlan(eememBaseAddress, activeGameFile, out plan);
        }

        Dictionary<long, OutbreakTrackerMemoryDomains> pages = new();
        foreach (OutbreakTrackerMemoryRegionDefinition region in regions)
        {
            if (region.ByteLength == 0)
            {
                continue;
            }

            long regionStart = region.BaseAddress;
            long regionEndInclusive = checked(regionStart + (long)region.ByteLength - 1);
            long firstPage = AlignDownToPage(regionStart);
            long lastPage = AlignDownToPage(regionEndInclusive);

            for (long page = firstPage; page <= lastPage; page += PageSizeBytes)
            {
                if (pages.TryGetValue(page, out OutbreakTrackerMemoryDomains existingDomains))
                {
                    pages[page] = existingDomains | region.Domains;
                }
                else
                {
                    pages[page] = region.Domains;
                }
            }
        }

        if (pages.Count == 0)
        {
            plan = [];
            return false;
        }

        plan = pages
            .OrderBy(static entry => entry.Key)
            .Select(static entry => new OutbreakTrackerMemoryWatchPlanItem(
                $"Page0x{entry.Key:X}",
                MemoryRegionSpec.Absolute((nint)entry.Key, (nuint)PageSizeBytes),
                entry.Value
            ))
            .ToArray();
        return plan.Length > 0;
    }

    private static long AlignDownToPage(long address) => address & ~(PageSizeBytes - 1);

    private static bool TryCreateHardwareWatchpointPlan(
        nint eememBaseAddress,
        GameFile activeGameFile,
        out OutbreakTrackerMemoryWatchPlanItem[] plan
    )
    {
        if (eememBaseAddress == nint.Zero)
        {
            plan = [];
            return false;
        }

        (string name, nint relativeOffset, MemoryWatchUnitPrecision unitPrecision, nuint elementSizeBytes) =
            activeGameFile switch
            {
                GameFile.FileOne => (
                    "FrameCounterFileOne",
                    FileOnePtrs.InGameFrameCounter,
                    MemoryWatchUnitPrecision.ByDWord,
                    (nuint)sizeof(int)
                ),
                GameFile.FileTwo => (
                    "FrameCounterFileTwo",
                    FileTwoPtrs.InGameFrameCounter,
                    MemoryWatchUnitPrecision.ByDWord,
                    (nuint)sizeof(int)
                ),
                _ => (
                    "FrameCounterProbe",
                    FileTwoPtrs.InGameFrameCounter,
                    MemoryWatchUnitPrecision.ByDWord,
                    (nuint)sizeof(int)
                ),
            };

        if (relativeOffset == nint.Zero)
        {
            plan = [];
            return false;
        }

        nint absoluteAddress = eememBaseAddress + relativeOffset;
        if (elementSizeBytes > 1)
        {
            long mask = (long)elementSizeBytes - 1;
            if ((((long)absoluteAddress) & mask) != 0)
            {
                plan = [];
                return false;
            }
        }

        plan =
        [
            new OutbreakTrackerMemoryWatchPlanItem(
                name,
                MemoryRegionSpec.Absolute(absoluteAddress, (nuint)sizeof(int), unitPrecision, elementSizeBytes),
                InGameWakeDomains
            ),
        ];

        return true;
    }
}

internal readonly record struct OutbreakTrackerMemoryWatchPlanItem(
    string Name,
    MemoryRegionSpec Region,
    OutbreakTrackerMemoryDomains Domains
);
