using MemoryWatcher;
using MemoryWatcher.Remote;
using OutbreakTracker2.Outbreak.Common;

namespace OutbreakTracker2.MemoryWatcherIntegration;

internal static class MemoryWatchProbePlanner
{
    private const nuint PageSizeBytes = 4096;
    private static readonly ScalarProbeCandidate[] ScalarCandidates =
    [
        new(FileTwoPtrs.InGameFrameCounter, sizeof(int), MemoryWatchUnitPrecision.ByDWord, sizeof(int)),
        new(FileOnePtrs.InGameFrameCounter, sizeof(int), MemoryWatchUnitPrecision.ByDWord, sizeof(int)),
        new(FileTwoPtrs.ScenarioStatus, sizeof(byte), MemoryWatchUnitPrecision.ByByte, sizeof(byte)),
        new(FileOnePtrs.ScenarioStatus, sizeof(byte), MemoryWatchUnitPrecision.ByByte, sizeof(byte)),
    ];

    private static readonly nint[] PageProbeCandidates =
    [
        FileTwoPtrs.InGameFrameCounter,
        FileOnePtrs.InGameFrameCounter,
        FileTwoPtrs.RoomPriority,
    ];

    public static MemoryRegionSpec CreateGroupedOt2Probe(IReadOnlyList<OutbreakTrackerMemoryRegionDefinition> regions)
    {
        ArgumentNullException.ThrowIfNull(regions);

        OutbreakTrackerMemoryRegionDefinition? region = SelectGroupedProbeRegion(regions);
        if (region is not OutbreakTrackerMemoryRegionDefinition selected)
        {
            throw new InvalidOperationException("No OT2 grouped region is available for negotiation probing.");
        }

        return MemoryRegionSpec.Absolute(selected.BaseAddress, selected.ByteLength);
    }

    public static MemoryRegionSpec CreateBackendProbe(
        WatchBackendKind backend,
        nint eememBaseAddress,
        IReadOnlyList<OutbreakTrackerMemoryRegionDefinition> regions
    )
    {
        ArgumentNullException.ThrowIfNull(regions);

        return backend switch
        {
            WatchBackendKind.HardwareWatchpoint => CreateScalarProbe(eememBaseAddress),
            WatchBackendKind.DirtyPage or WatchBackendKind.SoftDirty or WatchBackendKind.PageFault => CreatePageProbe(
                eememBaseAddress
            ),
            _ => CreateBlockProbe(regions),
        };
    }

    public static MemoryWatchSessionOptions CreateBackendProbeSessionOptions(
        MemoryWatcherSettings settings,
        WatchBackendKind backend,
        IReadOnlyList<int> hardwareThreadIds
    )
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new MemoryWatchSessionOptions
        {
            PreferredBackend = backend,
            PreferredPrecision = GetPreferredPrecision(backend),
            AllowFallback = false,
            AllowIntrusiveBackends = true,
            AllowNativeAgent = backend == WatchBackendKind.NativeAgent,
            HardwareThreadIds = hardwareThreadIds ?? Array.Empty<int>(),
            EventBufferCapacity = settings.EventBufferCapacity,
            HashBlockSizeBytes = settings.HashBlockSizeBytes,
            UseHashIndex = settings.UseHashIndex,
        };
    }

    public static MemoryWatchCapabilityNegotiationResult MergeWithBackendProbes(
        MemoryWatchCapabilityNegotiationResult groupedResult,
        IReadOnlyDictionary<WatchBackendKind, MemoryWatchNegotiatedCapability> backendProbeCapabilities,
        IReadOnlySet<WatchBackendKind>? ot2ReadyBackends = null
    )
    {
        ArgumentNullException.ThrowIfNull(groupedResult);
        ArgumentNullException.ThrowIfNull(backendProbeCapabilities);

        MemoryWatchNegotiatedCapability[] merged = new MemoryWatchNegotiatedCapability[
            groupedResult.Capabilities.Count
        ];
        for (int i = 0; i < groupedResult.Capabilities.Count; i++)
        {
            MemoryWatchNegotiatedCapability grouped = groupedResult.Capabilities[i];
            merged[i] = backendProbeCapabilities.TryGetValue(
                grouped.Backend,
                out MemoryWatchNegotiatedCapability? probe
            )
                ? MergeCapability(grouped, probe, ot2ReadyBackends)
                : grouped;
        }

        return new MemoryWatchCapabilityNegotiationResult
        {
            Host = groupedResult.Host,
            Target = groupedResult.Target,
            Capabilities = merged,
        };
    }

    private static MemoryWatchNegotiatedCapability MergeCapability(
        MemoryWatchNegotiatedCapability grouped,
        MemoryWatchNegotiatedCapability probe,
        IReadOnlySet<WatchBackendKind>? ot2ReadyBackends
    )
    {
        bool backendReadyOnProbe = probe.CurrentRequestAvailable;
        bool ot2CanUseProbeShape =
            backendReadyOnProbe
            && (
                ot2ReadyBackends?.Contains(grouped.Backend)
                ?? MemoryWatchActivityPlanFactory.SupportsDedicatedActivity(grouped.Backend)
            );
        bool useProbeFailureForCurrentRequest = !backendReadyOnProbe && ShouldUseProbeFailureForCurrentRequest(probe);
        bool currentRequestPromotable =
            grouped.CurrentRequestAvailable || IsOt2ShapeConstraint(grouped.CurrentRequestConstraintKind);
        bool currentRequestAvailable =
            grouped.CurrentRequestAvailable || (ot2CanUseProbeShape && currentRequestPromotable);
        WatchCapability? capability = grouped.CurrentCapability ?? probe.CurrentCapability;
        MemoryCapabilityConstraintKind environmentConstraint = backendReadyOnProbe
            ? MemoryCapabilityConstraintKind.None
            : GetProbeConstraint(probe);
        string? environmentReason = backendReadyOnProbe
            ? "Host and attached target can create this backend when OT2 supplies a backend-appropriate probe region."
            : probe.CurrentRequestReason ?? probe.EnvironmentSupportReason;

        return grouped with
        {
            BackendName = capability?.BackendName ?? grouped.BackendName,
            Invasiveness = probe.Invasiveness,
            PrecisionClass = probe.PrecisionClass,
            LatencyClass = probe.LatencyClass,
            EnvironmentSupport = backendReadyOnProbe
                ? MemoryCapabilitySupportLevel.Supported
                : probe.EnvironmentSupport,
            EnvironmentConstraintKind = environmentConstraint,
            EnvironmentSupportReason = environmentReason,
            CurrentCapability = capability,
            CurrentRequestAvailable = currentRequestAvailable,
            CurrentRequestConstraintKind =
                currentRequestAvailable ? MemoryCapabilityConstraintKind.None
                : useProbeFailureForCurrentRequest ? probe.CurrentRequestConstraintKind
                : grouped.CurrentRequestConstraintKind,
            CurrentRequestReason =
                currentRequestAvailable ? null
                : useProbeFailureForCurrentRequest ? probe.CurrentRequestReason
                : grouped.CurrentRequestReason,
        };
    }

    private static bool ShouldUseProbeFailureForCurrentRequest(MemoryWatchNegotiatedCapability probe)
    {
        if (
            probe.CurrentRequestConstraintKind
            is not MemoryCapabilityConstraintKind.None
                and not MemoryCapabilityConstraintKind.BackendNotSurfacedForRegion
        )
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(probe.CurrentRequestReason)
            && probe.CurrentRequestReason.StartsWith("Live probe creation failed:", StringComparison.Ordinal);
    }

    private static MemoryCapabilityConstraintKind GetProbeConstraint(MemoryWatchNegotiatedCapability probe)
    {
        if (
            probe.CurrentRequestConstraintKind
            is not MemoryCapabilityConstraintKind.None
                and not MemoryCapabilityConstraintKind.BackendNotSurfacedForRegion
        )
        {
            return probe.CurrentRequestConstraintKind;
        }

        return probe.EnvironmentConstraintKind;
    }

    private static bool IsOt2ShapeConstraint(MemoryCapabilityConstraintKind constraintKind)
    {
        return constraintKind
            is MemoryCapabilityConstraintKind.None
                or MemoryCapabilityConstraintKind.BackendNotSurfacedForRegion
                or MemoryCapabilityConstraintKind.RequiresScalarAlignedRegion
                or MemoryCapabilityConstraintKind.UnsupportedIntent;
    }

    private static MemoryRegionSpec CreateBlockProbe(IReadOnlyList<OutbreakTrackerMemoryRegionDefinition> regions)
    {
        OutbreakTrackerMemoryRegionDefinition? region = SelectGroupedProbeRegion(regions);
        if (region is not OutbreakTrackerMemoryRegionDefinition selected)
        {
            throw new InvalidOperationException("No OT2 grouped region is available for block probing.");
        }

        nuint probeLength = selected.ByteLength > PageSizeBytes ? PageSizeBytes : selected.ByteLength;
        return MemoryRegionSpec.Absolute(selected.BaseAddress, probeLength);
    }

    private static MemoryRegionSpec CreateScalarProbe(nint eememBaseAddress)
    {
        foreach (ScalarProbeCandidate candidate in ScalarCandidates)
        {
            nint address = eememBaseAddress + candidate.RelativeOffset;
            if (!IsAligned(address, candidate.AlignmentBytes))
            {
                continue;
            }

            return MemoryRegionSpec.Absolute(
                address,
                candidate.ByteLength,
                candidate.UnitPrecision,
                candidate.PreferredElementSizeBytes
            );
        }

        throw new InvalidOperationException("No aligned scalar OT2 probe candidate is available.");
    }

    private static MemoryRegionSpec CreatePageProbe(nint eememBaseAddress)
    {
        foreach (nint relativeOffset in PageProbeCandidates)
        {
            nint absoluteAddress = eememBaseAddress + relativeOffset;
            long absoluteAddressValue = absoluteAddress;
            long pageAlignedAddress = absoluteAddressValue & ~((long)PageSizeBytes - 1);
            if (pageAlignedAddress < (long)eememBaseAddress)
            {
                continue;
            }

            return MemoryRegionSpec.Absolute((nint)pageAlignedAddress, PageSizeBytes);
        }

        throw new InvalidOperationException("No OT2 page probe candidate is available.");
    }

    private static OutbreakTrackerMemoryRegionDefinition? SelectGroupedProbeRegion(
        IReadOnlyList<OutbreakTrackerMemoryRegionDefinition> regions
    )
    {
        for (int i = 0; i < regions.Count; i++)
        {
            OutbreakTrackerMemoryRegionDefinition region = regions[i];
            if (
                (
                    region.Domains
                    & (
                        OutbreakTrackerMemoryDomains.InGamePlayers
                        | OutbreakTrackerMemoryDomains.Enemies
                        | OutbreakTrackerMemoryDomains.Doors
                    )
                ) != OutbreakTrackerMemoryDomains.None
            )
            {
                return region;
            }
        }

        return regions.Count > 0 ? regions[0] : null;
    }

    private static bool IsAligned(nint address, nuint alignmentBytes)
    {
        if (alignmentBytes <= 1)
        {
            return true;
        }

        long mask = (long)alignmentBytes - 1;
        return (((long)address) & mask) == 0;
    }

    private static WatchPrecision GetPreferredPrecision(WatchBackendKind backend) =>
        backend switch
        {
            WatchBackendKind.HardwareWatchpoint => WatchPrecision.HardwareAddressExact,
            WatchBackendKind.DirtyPage => WatchPrecision.DirtyPageThenBitDiff,
            WatchBackendKind.SoftDirty => WatchPrecision.SoftDirtyThenBitDiff,
            WatchBackendKind.PageFault => WatchPrecision.PageFaultThenBitDiff,
            WatchBackendKind.NativeAgent or WatchBackendKind.DirtyRange => WatchPrecision.DirtyRangeThenBitDiff,
            _ => WatchPrecision.SnapshotBitExact,
        };

    private readonly record struct ScalarProbeCandidate(
        nint RelativeOffset,
        nuint ByteLength,
        MemoryWatchUnitPrecision UnitPrecision,
        nuint PreferredElementSizeBytes
    )
    {
        public nuint AlignmentBytes => PreferredElementSizeBytes;
    }
}
