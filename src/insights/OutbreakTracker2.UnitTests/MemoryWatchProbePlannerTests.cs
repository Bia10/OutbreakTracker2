using MemoryWatcher;
using MemoryWatcher.Remote;
using OutbreakTracker2.MemoryWatcherIntegration;
using OutbreakTracker2.Outbreak.Common;

namespace OutbreakTracker2.UnitTests;

public sealed class MemoryWatchProbePlannerTests
{
    [Test]
    public async Task CreateBackendProbe_HardwareWatchpoint_UsesAlignedScalarFrameCounter()
    {
        IReadOnlyList<OutbreakTrackerMemoryRegionDefinition> regions =
            new OutbreakTrackerMemoryRegionCatalog().CreateRegions(nint.Zero);

        MemoryRegionSpec probe = MemoryWatchProbePlanner.CreateBackendProbe(
            WatchBackendKind.HardwareWatchpoint,
            nint.Zero,
            regions
        );

        await Assert.That(probe.Address).IsEqualTo(FileTwoPtrs.InGameFrameCounter);
        await Assert.That(probe.ByteLength).IsEqualTo((nuint)sizeof(int));
        await Assert.That(probe.UnitPrecision).IsEqualTo(MemoryWatchUnitPrecision.ByDWord);
        await Assert.That(probe.PreferredElementSizeBytes).IsEqualTo((nuint)sizeof(int));
    }

    [Test]
    public async Task CreateBackendProbe_PageFault_UsesPageAlignedProbe()
    {
        IReadOnlyList<OutbreakTrackerMemoryRegionDefinition> regions =
            new OutbreakTrackerMemoryRegionCatalog().CreateRegions(nint.Zero);

        MemoryRegionSpec probe = MemoryWatchProbePlanner.CreateBackendProbe(
            WatchBackendKind.PageFault,
            nint.Zero,
            regions
        );

        await Assert.That(((long)probe.Address & 0xFFF)).IsEqualTo(0L);
        await Assert.That(probe.ByteLength).IsEqualTo((nuint)4096);
    }

    [Test]
    public async Task CreateBackendProbeSessionOptions_ForHostProbe_ForcesSpecificBackendWithoutFallback()
    {
        MemoryWatcherSettings settings = new()
        {
            AllowIntrusiveBackends = false,
            EventBufferCapacity = 2048,
            HashBlockSizeBytes = 128,
            UseHashIndex = false,
        };

        MemoryWatchSessionOptions options = MemoryWatchProbePlanner.CreateBackendProbeSessionOptions(
            settings,
            WatchBackendKind.HardwareWatchpoint,
            [1, 2, 3]
        );

        await Assert.That(options.PreferredBackend).IsEqualTo(WatchBackendKind.HardwareWatchpoint);
        await Assert.That(options.PreferredPrecision).IsEqualTo(WatchPrecision.HardwareAddressExact);
        await Assert.That(options.AllowFallback).IsFalse();
        await Assert.That(options.AllowIntrusiveBackends).IsTrue();
        await Assert.That(options.HardwareThreadIds.Count).IsEqualTo(3);
        await Assert.That(options.EventBufferCapacity).IsEqualTo(2048);
        await Assert.That(options.HashBlockSizeBytes).IsEqualTo(128);
        await Assert.That(options.UseHashIndex).IsFalse();
    }

    [Test]
    public async Task MergeWithBackendProbes_PromotesHardwareWatchpointWhenOt2HasDedicatedPointWatchPlan()
    {
        MemoryWatchNegotiatedCapability groupedCapability = new()
        {
            Backend = WatchBackendKind.HardwareWatchpoint,
            BackendName = "hardware-watchpoint",
            Invasiveness = MemoryObservationInvasiveness.OperatingSystemHook,
            PrecisionClass = MemoryObservationPrecisionClass.SignaledFinalValue,
            LatencyClass = MemoryObservationLatencyClass.Under1Millisecond,
            EnvironmentSupport = MemoryCapabilitySupportLevel.Conditional,
            EnvironmentConstraintKind = MemoryCapabilityConstraintKind.MissingHardwareThreadIds,
            EnvironmentSupportReason = "Hardware watchpoints are armed per thread.",
            CurrentCapability = null,
            CurrentRequestAvailable = false,
            CurrentRequestConstraintKind = MemoryCapabilityConstraintKind.RequiresScalarAlignedRegion,
            CurrentRequestReason = "Grouped region is too large for watchpoint arming.",
        };

        WatchCapability probeCapability = new()
        {
            BackendName = "hardware-watchpoint",
            Precision = WatchPrecision.HardwareAddressExact,
            Safety = WatchSafety.SafePublicNativeInternals,
            IsAvailable = true,
            EventDriven = true,
            Intrusive = true,
            RequiresAgent = false,
            RequiresUnsafe = false,
            BitExact = true,
            EdgeExact = true,
            CanMissAbaBetweenSamples = false,
            TriggerGranularityBits = 8,
            AlignmentRequirementBytes = 4,
            RequiresThreadIdList = true,
        };

        MemoryWatchNegotiatedCapability probeNegotiation = new()
        {
            Backend = WatchBackendKind.HardwareWatchpoint,
            BackendName = "hardware-watchpoint",
            Invasiveness = MemoryObservationInvasiveness.OperatingSystemHook,
            PrecisionClass = MemoryObservationPrecisionClass.TransientEdgeExact,
            LatencyClass = MemoryObservationLatencyClass.Under1Millisecond,
            EnvironmentSupport = MemoryCapabilitySupportLevel.Supported,
            EnvironmentConstraintKind = MemoryCapabilityConstraintKind.None,
            EnvironmentSupportReason = "Platform support is present.",
            CurrentCapability = probeCapability,
            CurrentRequestAvailable = true,
            CurrentRequestConstraintKind = MemoryCapabilityConstraintKind.None,
            CurrentRequestReason = null,
        };

        MemoryWatchCapabilityNegotiationResult groupedResult = new()
        {
            Host = new MemoryWatchHostEnvironment { OperatingSystem = "TestOS" },
            Target = new MemoryWatchTargetEnvironment
            {
                ProcessId = 1,
                ProcessName = "pcsx2",
                ProcessFound = true,
                SessionOpened = true,
            },
            Capabilities = [groupedCapability],
        };

        MemoryWatchCapabilityNegotiationResult merged = MemoryWatchProbePlanner.MergeWithBackendProbes(
            groupedResult,
            new Dictionary<WatchBackendKind, MemoryWatchNegotiatedCapability>
            {
                [WatchBackendKind.HardwareWatchpoint] = probeNegotiation,
            },
            new HashSet<WatchBackendKind> { WatchBackendKind.HardwareWatchpoint }
        );

        MemoryWatchNegotiatedCapability capability = merged.Capabilities.Single();
        await Assert.That(capability.EnvironmentSupport).IsEqualTo(MemoryCapabilitySupportLevel.Supported);
        await Assert.That(capability.EnvironmentConstraintKind).IsEqualTo(MemoryCapabilityConstraintKind.None);
        await Assert.That(capability.CurrentRequestAvailable).IsTrue();
        await Assert.That(capability.CurrentRequestConstraintKind).IsEqualTo(MemoryCapabilityConstraintKind.None);
        await Assert.That(capability.CurrentRequestReason).IsNull();
        await Assert.That(capability.CurrentCapability).IsEqualTo(probeCapability);
        await Assert.That(capability.PrecisionClass).IsEqualTo(MemoryObservationPrecisionClass.TransientEdgeExact);
    }

    [Test]
    public async Task MergeWithBackendProbes_PromotesDirtyPageToReadyWhenOt2HasDedicatedPagePlan()
    {
        MemoryWatchNegotiatedCapability groupedCapability = new()
        {
            Backend = WatchBackendKind.DirtyPage,
            BackendName = "dirty-page-then-diff",
            Invasiveness = MemoryObservationInvasiveness.OperatingSystemHook,
            PrecisionClass = MemoryObservationPrecisionClass.SignaledFinalValue,
            LatencyClass = MemoryObservationLatencyClass.Under1Millisecond,
            EnvironmentSupport = MemoryCapabilitySupportLevel.Conditional,
            EnvironmentConstraintKind = MemoryCapabilityConstraintKind.RequiresPageTracker,
            EnvironmentSupportReason = "Page-tracker path depends on a page-aligned request.",
            CurrentCapability = null,
            CurrentRequestAvailable = false,
            CurrentRequestConstraintKind = MemoryCapabilityConstraintKind.BackendNotSurfacedForRegion,
            CurrentRequestReason = "Grouped OT2 region is not page-shaped.",
        };

        WatchCapability probeCapability = new()
        {
            BackendName = "dirty-page-then-diff",
            Precision = WatchPrecision.DirtyPageThenBitDiff,
            Safety = WatchSafety.SafePublicNativeInternals,
            IsAvailable = true,
            EventDriven = true,
            Intrusive = true,
            RequiresAgent = false,
            RequiresUnsafe = false,
            BitExact = true,
            EdgeExact = false,
            CanMissAbaBetweenSamples = true,
            TriggerGranularityBits = 4096 * 8,
            AlignmentRequirementBytes = 4096,
            RequiresThreadIdList = false,
        };

        MemoryWatchNegotiatedCapability probeNegotiation = new()
        {
            Backend = WatchBackendKind.DirtyPage,
            BackendName = "dirty-page-then-diff",
            Invasiveness = MemoryObservationInvasiveness.OperatingSystemHook,
            PrecisionClass = MemoryObservationPrecisionClass.SignaledFinalValue,
            LatencyClass = MemoryObservationLatencyClass.Under1Millisecond,
            EnvironmentSupport = MemoryCapabilitySupportLevel.Supported,
            EnvironmentConstraintKind = MemoryCapabilityConstraintKind.None,
            EnvironmentSupportReason = "Platform support is present.",
            CurrentCapability = probeCapability,
            CurrentRequestAvailable = true,
            CurrentRequestConstraintKind = MemoryCapabilityConstraintKind.None,
            CurrentRequestReason = null,
        };

        MemoryWatchCapabilityNegotiationResult groupedResult = new()
        {
            Host = new MemoryWatchHostEnvironment { OperatingSystem = "TestOS" },
            Target = new MemoryWatchTargetEnvironment
            {
                ProcessId = 1,
                ProcessName = "pcsx2",
                ProcessFound = true,
                SessionOpened = true,
            },
            Capabilities = [groupedCapability],
        };

        MemoryWatchCapabilityNegotiationResult merged = MemoryWatchProbePlanner.MergeWithBackendProbes(
            groupedResult,
            new Dictionary<WatchBackendKind, MemoryWatchNegotiatedCapability>
            {
                [WatchBackendKind.DirtyPage] = probeNegotiation,
            },
            new HashSet<WatchBackendKind> { WatchBackendKind.DirtyPage }
        );

        MemoryWatchNegotiatedCapability capability = merged.Capabilities.Single();
        await Assert.That(capability.EnvironmentSupport).IsEqualTo(MemoryCapabilitySupportLevel.Supported);
        await Assert.That(capability.CurrentRequestAvailable).IsTrue();
        await Assert.That(capability.CurrentRequestConstraintKind).IsEqualTo(MemoryCapabilityConstraintKind.None);
        await Assert.That(capability.CurrentRequestReason).IsNull();
        await Assert.That(capability.CurrentCapability).IsEqualTo(probeCapability);
    }

    [Test]
    public async Task MergeWithBackendProbes_PromotesPageFaultToReadyWhenOt2HasDedicatedPagePlan()
    {
        MemoryWatchNegotiatedCapability groupedCapability = new()
        {
            Backend = WatchBackendKind.PageFault,
            BackendName = "page-fault-then-diff",
            Invasiveness = MemoryObservationInvasiveness.OperatingSystemHook,
            PrecisionClass = MemoryObservationPrecisionClass.SignaledFinalValue,
            LatencyClass = MemoryObservationLatencyClass.Under1Millisecond,
            EnvironmentSupport = MemoryCapabilitySupportLevel.Conditional,
            EnvironmentConstraintKind = MemoryCapabilityConstraintKind.RequiresPageTracker,
            EnvironmentSupportReason = "Page-fault wakeups depend on a page-shaped request.",
            CurrentCapability = null,
            CurrentRequestAvailable = false,
            CurrentRequestConstraintKind = MemoryCapabilityConstraintKind.BackendNotSurfacedForRegion,
            CurrentRequestReason = "Grouped OT2 region is not page-shaped.",
        };

        WatchCapability probeCapability = new()
        {
            BackendName = "page-fault-then-diff",
            Precision = WatchPrecision.PageFaultThenBitDiff,
            Safety = WatchSafety.SafePublicNativeInternals,
            IsAvailable = true,
            EventDriven = true,
            Intrusive = true,
            RequiresAgent = false,
            RequiresUnsafe = false,
            BitExact = true,
            EdgeExact = false,
            CanMissAbaBetweenSamples = true,
            TriggerGranularityBits = 4096 * 8,
            AlignmentRequirementBytes = 4096,
            RequiresThreadIdList = false,
        };

        MemoryWatchNegotiatedCapability probeNegotiation = new()
        {
            Backend = WatchBackendKind.PageFault,
            BackendName = "page-fault-then-diff",
            Invasiveness = MemoryObservationInvasiveness.OperatingSystemHook,
            PrecisionClass = MemoryObservationPrecisionClass.SignaledFinalValue,
            LatencyClass = MemoryObservationLatencyClass.Under1Millisecond,
            EnvironmentSupport = MemoryCapabilitySupportLevel.Supported,
            EnvironmentConstraintKind = MemoryCapabilityConstraintKind.None,
            EnvironmentSupportReason = "Platform support is present.",
            CurrentCapability = probeCapability,
            CurrentRequestAvailable = true,
            CurrentRequestConstraintKind = MemoryCapabilityConstraintKind.None,
            CurrentRequestReason = null,
        };

        MemoryWatchCapabilityNegotiationResult groupedResult = new()
        {
            Host = new MemoryWatchHostEnvironment { OperatingSystem = "TestOS" },
            Target = new MemoryWatchTargetEnvironment
            {
                ProcessId = 1,
                ProcessName = "pcsx2",
                ProcessFound = true,
                SessionOpened = true,
            },
            Capabilities = [groupedCapability],
        };

        MemoryWatchCapabilityNegotiationResult merged = MemoryWatchProbePlanner.MergeWithBackendProbes(
            groupedResult,
            new Dictionary<WatchBackendKind, MemoryWatchNegotiatedCapability>
            {
                [WatchBackendKind.PageFault] = probeNegotiation,
            },
            new HashSet<WatchBackendKind> { WatchBackendKind.PageFault }
        );

        MemoryWatchNegotiatedCapability capability = merged.Capabilities.Single();
        await Assert.That(capability.EnvironmentSupport).IsEqualTo(MemoryCapabilitySupportLevel.Supported);
        await Assert.That(capability.CurrentRequestAvailable).IsTrue();
        await Assert.That(capability.CurrentRequestConstraintKind).IsEqualTo(MemoryCapabilityConstraintKind.None);
        await Assert.That(capability.CurrentRequestReason).IsNull();
        await Assert.That(capability.CurrentCapability).IsEqualTo(probeCapability);
    }

    [Test]
    public async Task MergeWithBackendProbes_PrefersProbeFailureOverGroupedShapeReason()
    {
        MemoryWatchNegotiatedCapability groupedCapability = new()
        {
            Backend = WatchBackendKind.PageFault,
            BackendName = "page-fault-then-diff",
            Invasiveness = MemoryObservationInvasiveness.OperatingSystemHook,
            PrecisionClass = MemoryObservationPrecisionClass.SignaledFinalValue,
            LatencyClass = MemoryObservationLatencyClass.Under1Millisecond,
            EnvironmentSupport = MemoryCapabilitySupportLevel.Conditional,
            EnvironmentConstraintKind = MemoryCapabilityConstraintKind.RequiresPageTracker,
            EnvironmentSupportReason = "Page-fault wakeups depend on a page-shaped request.",
            CurrentCapability = null,
            CurrentRequestAvailable = false,
            CurrentRequestConstraintKind = MemoryCapabilityConstraintKind.BackendNotSurfacedForRegion,
            CurrentRequestReason = "Grouped OT2 region is not page-shaped.",
        };

        MemoryWatchNegotiatedCapability probeNegotiation = new()
        {
            Backend = WatchBackendKind.PageFault,
            BackendName = "page-fault-then-diff",
            Invasiveness = MemoryObservationInvasiveness.OperatingSystemHook,
            PrecisionClass = MemoryObservationPrecisionClass.SignaledFinalValue,
            LatencyClass = MemoryObservationLatencyClass.Under1Millisecond,
            EnvironmentSupport = MemoryCapabilitySupportLevel.Conditional,
            EnvironmentConstraintKind = MemoryCapabilityConstraintKind.Unknown,
            EnvironmentSupportReason = "Live probe creation failed: Access denied while arming PAGE_GUARD.",
            CurrentCapability = null,
            CurrentRequestAvailable = false,
            CurrentRequestConstraintKind = MemoryCapabilityConstraintKind.Unknown,
            CurrentRequestReason = "Live probe creation failed: Access denied while arming PAGE_GUARD.",
        };

        MemoryWatchCapabilityNegotiationResult groupedResult = new()
        {
            Host = new MemoryWatchHostEnvironment { OperatingSystem = "TestOS" },
            Target = new MemoryWatchTargetEnvironment
            {
                ProcessId = 1,
                ProcessName = "pcsx2",
                ProcessFound = true,
                SessionOpened = true,
            },
            Capabilities = [groupedCapability],
        };

        MemoryWatchCapabilityNegotiationResult merged = MemoryWatchProbePlanner.MergeWithBackendProbes(
            groupedResult,
            new Dictionary<WatchBackendKind, MemoryWatchNegotiatedCapability>
            {
                [WatchBackendKind.PageFault] = probeNegotiation,
            },
            new HashSet<WatchBackendKind> { WatchBackendKind.PageFault }
        );

        MemoryWatchNegotiatedCapability capability = merged.Capabilities.Single();
        await Assert.That(capability.CurrentRequestAvailable).IsFalse();
        await Assert.That(capability.CurrentRequestConstraintKind).IsEqualTo(MemoryCapabilityConstraintKind.Unknown);
        await Assert
            .That(capability.CurrentRequestReason)
            .IsEqualTo("Live probe creation failed: Access denied while arming PAGE_GUARD.");
    }
}
