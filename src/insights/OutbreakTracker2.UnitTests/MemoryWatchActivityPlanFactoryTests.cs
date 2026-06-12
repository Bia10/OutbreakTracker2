using MemoryWatcher;
using MemoryWatcher.Remote;
using OutbreakTracker2.MemoryWatcherIntegration;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;

namespace OutbreakTracker2.UnitTests;

public sealed class MemoryWatchActivityPlanFactoryTests
{
    [Test]
    public async Task CreateReadPlan_PreservesRegionIdentityAndDomains()
    {
        IReadOnlyList<OutbreakTrackerMemoryRegionDefinition> regions =
        [
            new("ScenarioA", (nint)0x1000, (nuint)64, OutbreakTrackerMemoryDomains.Scenario),
            new("EnemyA", (nint)0x2004, (nuint)32, OutbreakTrackerMemoryDomains.Enemies),
        ];

        OutbreakTrackerMemoryWatchPlanItem[] plan = MemoryWatchActivityPlanFactory.CreateReadPlan(regions);

        await Assert.That(plan.Length).IsEqualTo(2);
        await Assert.That(plan[0].Name).IsEqualTo("ScenarioA");
        await Assert.That(plan[0].Region.Address).IsEqualTo((nint)0x1000);
        await Assert.That(plan[0].Region.ByteLength).IsEqualTo((nuint)64);
        await Assert.That(plan[0].Domains).IsEqualTo(OutbreakTrackerMemoryDomains.Scenario);
        await Assert.That(plan[1].Name).IsEqualTo("EnemyA");
        await Assert.That(plan[1].Domains).IsEqualTo(OutbreakTrackerMemoryDomains.Enemies);
    }

    [Test]
    public async Task TryCreateActivityPlan_DirtyPage_MergesOverlappingRegionsIntoDistinctPages()
    {
        IReadOnlyList<OutbreakTrackerMemoryRegionDefinition> regions =
        [
            new("ScenarioA", (nint)0x1000, (nuint)64, OutbreakTrackerMemoryDomains.Scenario),
            new("PlayerA", (nint)0x10F0, (nuint)64, OutbreakTrackerMemoryDomains.InGamePlayers),
            new("EnemyA", (nint)0x2004, (nuint)32, OutbreakTrackerMemoryDomains.Enemies),
        ];

        bool created = MemoryWatchActivityPlanFactory.TryCreateActivityPlan(
            WatchBackendKind.DirtyPage,
            regions,
            out OutbreakTrackerMemoryWatchPlanItem[] plan
        );

        await Assert.That(created).IsTrue();
        await Assert.That(plan.Length).IsEqualTo(2);
        await Assert.That(plan[0].Region.Address).IsEqualTo((nint)0x1000);
        await Assert.That(plan[0].Region.ByteLength).IsEqualTo((nuint)4096);
        await Assert
            .That(plan[0].Domains)
            .IsEqualTo(OutbreakTrackerMemoryDomains.Scenario | OutbreakTrackerMemoryDomains.InGamePlayers);
        await Assert.That(plan[1].Region.Address).IsEqualTo((nint)0x2000);
        await Assert.That(plan[1].Domains).IsEqualTo(OutbreakTrackerMemoryDomains.Enemies);
    }

    [Test]
    public async Task TryCreateActivityPlan_HardwareWatchpoint_UsesActiveFileFrameCounterSentinel()
    {
        IReadOnlyList<OutbreakTrackerMemoryRegionDefinition> regions =
            new OutbreakTrackerMemoryRegionCatalog().CreateRegions((nint)0x7000_0000);

        bool created = MemoryWatchActivityPlanFactory.TryCreateActivityPlan(
            WatchBackendKind.HardwareWatchpoint,
            (nint)0x7000_0000,
            GameFile.FileTwo,
            regions,
            out OutbreakTrackerMemoryWatchPlanItem[] plan
        );

        await Assert.That(created).IsTrue();
        await Assert.That(plan.Length).IsEqualTo(1);
        await Assert.That(plan[0].Name).IsEqualTo("FrameCounterFileTwo");
        await Assert.That(plan[0].Region.Address).IsEqualTo((nint)0x7000_0000 + FileTwoPtrs.InGameFrameCounter);
        await Assert.That(plan[0].Region.ByteLength).IsEqualTo((nuint)sizeof(int));
        await Assert.That(plan[0].Region.UnitPrecision).IsEqualTo(MemoryWatchUnitPrecision.ByDWord);
        await Assert.That(plan[0].Region.PreferredElementSizeBytes).IsEqualTo((nuint)sizeof(int));
        await Assert
            .That(plan[0].Domains)
            .IsEqualTo(
                OutbreakTrackerMemoryDomains.Scenario
                    | OutbreakTrackerMemoryDomains.InGamePlayers
                    | OutbreakTrackerMemoryDomains.Enemies
                    | OutbreakTrackerMemoryDomains.Doors
            );
    }

    [Test]
    public async Task TryCreateActivityPlan_HardwareWatchpoint_FallsBackToGenericProbeWhenGameFileIsUnknown()
    {
        IReadOnlyList<OutbreakTrackerMemoryRegionDefinition> regions =
            new OutbreakTrackerMemoryRegionCatalog().CreateRegions((nint)0x7000_0000);

        bool created = MemoryWatchActivityPlanFactory.TryCreateActivityPlan(
            WatchBackendKind.HardwareWatchpoint,
            (nint)0x7000_0000,
            GameFile.Unknown,
            regions,
            out OutbreakTrackerMemoryWatchPlanItem[] plan
        );

        await Assert.That(created).IsTrue();
        await Assert.That(plan.Length).IsEqualTo(1);
        await Assert.That(plan[0].Name).IsEqualTo("FrameCounterProbe");
        await Assert.That(plan[0].Region.Address).IsEqualTo((nint)0x7000_0000 + FileTwoPtrs.InGameFrameCounter);
        await Assert.That(plan[0].Region.ByteLength).IsEqualTo((nuint)sizeof(int));
        await Assert.That(plan[0].Region.UnitPrecision).IsEqualTo(MemoryWatchUnitPrecision.ByDWord);
        await Assert.That(plan[0].Region.PreferredElementSizeBytes).IsEqualTo((nuint)sizeof(int));
    }

    [Test]
    public async Task TryCreateActivityPlan_Snapshot_ReturnsFalse()
    {
        bool created = MemoryWatchActivityPlanFactory.TryCreateActivityPlan(
            WatchBackendKind.Snapshot,
            [new("Scenario", (nint)0x1000, (nuint)32, OutbreakTrackerMemoryDomains.Scenario)],
            out OutbreakTrackerMemoryWatchPlanItem[] plan
        );

        await Assert.That(created).IsFalse();
        await Assert.That(plan).IsEmpty();
    }
}
