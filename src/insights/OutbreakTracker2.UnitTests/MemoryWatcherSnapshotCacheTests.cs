using System.Reflection;
using MemoryWatcher;
using MemoryWatcher.Remote;
using Microsoft.Extensions.Logging.Abstractions;
using OutbreakTracker2.MemoryWatcherIntegration;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;

namespace OutbreakTracker2.UnitTests;

public sealed class MemoryWatcherSnapshotCacheTests
{
    [Test]
    public async Task CreateReadSessionOptions_ForHardwareWatchpoint_KeepsGroupedReadsSnapshotSafe()
    {
        MemoryWatcherSettings settings = new()
        {
            PreferredBackend = WatchBackendKind.HardwareWatchpoint,
            PreferredPrecision = WatchPrecision.HardwareAddressExact,
            AllowFallback = false,
            AllowIntrusiveBackends = true,
            EventBufferCapacity = 2048,
            HashBlockSizeBytes = 128,
            UseHashIndex = false,
        };

        MemoryWatcherSnapshotCache cache = new(
            new FakeMemoryWatchSessionFactory(),
            new OutbreakTrackerMemoryRegionCatalog(),
            settings,
            NullLogger<MemoryWatcherSnapshotCache>.Instance
        );

        MemoryWatchSessionOptions readOptions = InvokeNonPublic<MemoryWatchSessionOptions>(
            cache,
            "CreateReadSessionOptions",
            [new[] { 11, 22, 33 }]
        );

        await Assert.That(readOptions.PreferredBackend).IsEqualTo(WatchBackendKind.Auto);
        await Assert.That(readOptions.PreferredPrecision).IsEqualTo(WatchPrecision.SnapshotBitExact);
        await Assert.That(readOptions.AllowFallback).IsTrue();
        await Assert.That(readOptions.AllowIntrusiveBackends).IsFalse();
        await Assert.That(readOptions.AllowNativeAgent).IsFalse();
        await Assert.That(readOptions.HardwareThreadIds.Count).IsEqualTo(3);
        await Assert.That(readOptions.EventBufferCapacity).IsEqualTo(2048);
        await Assert.That(readOptions.HashBlockSizeBytes).IsEqualTo(128);
        await Assert.That(readOptions.UseHashIndex).IsFalse();
    }

    [Test]
    public async Task BuildActivityPipeline_PageFault_OpensDedicatedWakeSessionAlongsideGroupedSnapshots()
    {
        MemoryWatcherSettings settings = new()
        {
            PreferredBackend = WatchBackendKind.PageFault,
            PreferredPrecision = WatchPrecision.PageFaultThenBitDiff,
            AllowFallback = false,
            AllowIntrusiveBackends = true,
        };
        FakeMemoryWatchSessionFactory sessionFactory = new();
        OutbreakTrackerMemoryRegionCatalog regionCatalog = new();
        MemoryWatcherSnapshotCache cache = new(
            sessionFactory,
            regionCatalog,
            settings,
            NullLogger<MemoryWatcherSnapshotCache>.Instance
        );

        nint eememBaseAddress = (nint)0x7000_0000;
        IReadOnlyList<OutbreakTrackerMemoryRegionDefinition> regions = regionCatalog.CreateRegions(eememBaseAddress);
        OutbreakTrackerMemoryWatchPlanItem[] readPlan = MemoryWatchActivityPlanFactory.CreateReadPlan(regions);
        Array watchedRegions = CreateWatchedRegions(readPlan);

        object pipeline = InvokeNonPublic<object>(
            cache,
            "BuildActivityPipeline",
            [1234, Array.Empty<int>(), eememBaseAddress, GameFile.FileTwo, regions, watchedRegions]
        );

        await Assert.That(sessionFactory.OpenCalls.Count).IsEqualTo(1);
        await Assert.That(sessionFactory.OpenCalls[0].ProcessId).IsEqualTo(1234);
        await Assert.That(sessionFactory.OpenCalls[0].Options.PreferredBackend).IsEqualTo(WatchBackendKind.PageFault);
        await Assert
            .That(sessionFactory.OpenCalls[0].Options.PreferredPrecision)
            .IsEqualTo(WatchPrecision.PageFaultThenBitDiff);
        await Assert.That(sessionFactory.OpenCalls[0].Options.AllowIntrusiveBackends).IsTrue();

        object? activitySession = GetPropertyValue(pipeline, "Session");
        object? activitySource = GetPropertyValue(pipeline, "ActivitySource");
        Array dedicatedWatches = (Array)GetPropertyValue(pipeline, "DedicatedWatches")!;
        Array descriptors = (Array)GetPropertyValue(pipeline, "Descriptors")!;

        await Assert.That(activitySession).IsNotNull();
        await Assert.That(activitySource).IsNotNull();
        await Assert.That(dedicatedWatches.Length).IsGreaterThan(0);
        await Assert.That(descriptors.Length).IsEqualTo(readPlan.Length + dedicatedWatches.Length);

        int refreshDescriptorCount = descriptors
            .Cast<object>()
            .Count(descriptor => (bool)GetPropertyValue(descriptor, "RequiresSnapshotRefresh")!);
        await Assert.That(refreshDescriptorCount).IsEqualTo(dedicatedWatches.Length);

        object firstDedicatedWatch = dedicatedWatches.GetValue(0)!;
        string watchName = (string)GetPropertyValue(firstDedicatedWatch, "Name")!;
        FakeMemoryWatchHandle handle = (FakeMemoryWatchHandle)GetPropertyValue(firstDedicatedWatch, "Handle")!;
        await Assert.That(watchName.StartsWith("Page0x", StringComparison.Ordinal)).IsTrue();
        await Assert.That(handle.Region.ByteLength).IsEqualTo((nuint)4096);
        await Assert.That(((long)handle.Region.Address & 0xFFF)).IsEqualTo(0L);
    }

    [Test]
    public async Task BuildActivityPipeline_HardwareWatchpoint_UsesFrameCounterWakeSentinel()
    {
        MemoryWatcherSettings settings = new()
        {
            PreferredBackend = WatchBackendKind.HardwareWatchpoint,
            PreferredPrecision = WatchPrecision.HardwareAddressExact,
            AllowFallback = false,
            AllowIntrusiveBackends = true,
        };
        FakeMemoryWatchSessionFactory sessionFactory = new();
        OutbreakTrackerMemoryRegionCatalog regionCatalog = new();
        MemoryWatcherSnapshotCache cache = new(
            sessionFactory,
            regionCatalog,
            settings,
            NullLogger<MemoryWatcherSnapshotCache>.Instance
        );

        nint eememBaseAddress = (nint)0x7000_0000;
        IReadOnlyList<OutbreakTrackerMemoryRegionDefinition> regions = regionCatalog.CreateRegions(eememBaseAddress);
        OutbreakTrackerMemoryWatchPlanItem[] readPlan = MemoryWatchActivityPlanFactory.CreateReadPlan(regions);
        Array watchedRegions = CreateWatchedRegions(readPlan);

        object pipeline = InvokeNonPublic<object>(
            cache,
            "BuildActivityPipeline",
            [4321, new[] { 91, 92 }, eememBaseAddress, GameFile.FileTwo, regions, watchedRegions]
        );

        await Assert.That(sessionFactory.OpenCalls.Count).IsEqualTo(1);
        await Assert.That(sessionFactory.OpenCalls[0].ProcessId).IsEqualTo(4321);
        await Assert
            .That(sessionFactory.OpenCalls[0].Options.PreferredBackend)
            .IsEqualTo(WatchBackendKind.HardwareWatchpoint);
        await Assert
            .That(sessionFactory.OpenCalls[0].Options.PreferredPrecision)
            .IsEqualTo(WatchPrecision.HardwareAddressExact);
        await Assert.That(sessionFactory.OpenCalls[0].Options.HardwareThreadIds.Count).IsEqualTo(2);

        Array dedicatedWatches = (Array)GetPropertyValue(pipeline, "DedicatedWatches")!;
        await Assert.That(dedicatedWatches.Length).IsEqualTo(1);

        object hardwareWatch = dedicatedWatches.GetValue(0)!;
        string watchName = (string)GetPropertyValue(hardwareWatch, "Name")!;
        FakeMemoryWatchHandle handle = (FakeMemoryWatchHandle)GetPropertyValue(hardwareWatch, "Handle")!;

        await Assert.That(watchName).IsEqualTo("FrameCounterFileTwo");
        await Assert.That(handle.Region.Address).IsEqualTo(eememBaseAddress + FileTwoPtrs.InGameFrameCounter);
        await Assert.That(handle.Region.ByteLength).IsEqualTo((nuint)sizeof(int));
        await Assert.That(handle.Region.UnitPrecision).IsEqualTo(MemoryWatchUnitPrecision.ByDWord);
        await Assert.That(handle.Region.PreferredElementSizeBytes).IsEqualTo((nuint)sizeof(int));
    }

    private static Array CreateWatchedRegions(OutbreakTrackerMemoryWatchPlanItem[] readPlan)
    {
        Type watchedRegionType = typeof(MemoryWatcherSnapshotCache).GetNestedType(
            "WatchedRegion",
            BindingFlags.NonPublic
        )!;
        ConstructorInfo constructor = watchedRegionType
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Single();
        Array watchedRegions = Array.CreateInstance(watchedRegionType, readPlan.Length);

        for (int i = 0; i < readPlan.Length; i++)
        {
            IMemoryWatchHandle handle = new FakeMemoryWatchHandle(readPlan[i].Region);
            object watchedRegion = constructor.Invoke([
                readPlan[i],
                handle,
                new byte[checked((int)readPlan[i].Region.ByteLength)],
            ]);
            watchedRegions.SetValue(watchedRegion, i);
        }

        return watchedRegions;
    }

    private static T InvokeNonPublic<T>(object target, string methodName, object?[] args)
    {
        MethodInfo method =
            target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
            ?? throw new MissingMethodException(target.GetType().FullName, methodName);
        return (T)method.Invoke(target, args)!;
    }

    private static object? GetPropertyValue(object target, string propertyName) =>
        target
            .GetType()
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .GetValue(target);

    private sealed class FakeMemoryWatchSessionFactory : IMemoryWatchSessionFactory
    {
        public List<OpenCall> OpenCalls { get; } = [];

        public IMemoryWatchSession Open(int processId, MemoryWatchSessionOptions options)
        {
            FakeMemoryWatchSession session = new();
            OpenCalls.Add(new OpenCall(processId, options, session));
            return session;
        }
    }

    private sealed record OpenCall(int ProcessId, MemoryWatchSessionOptions Options, FakeMemoryWatchSession Session);

    private sealed class FakeMemoryWatchSession : IMemoryWatchSession
    {
        public List<FakeMemoryWatchHandle> CreatedHandles { get; } = [];

        public IReadOnlyList<WatchCapability> QueryCapabilities(MemoryRegionSpec region) => [];

        public ResolvedMemoryRegion Resolve(MemoryRegionSpec region) => default;

        public IMemoryWatchHandle CreateWatch(MemoryRegionSpec region)
        {
            FakeMemoryWatchHandle handle = new(region);
            CreatedHandles.Add(handle);
            return handle;
        }

        public void Dispose() { }
    }

    private sealed class FakeMemoryWatchHandle(MemoryRegionSpec region) : IMemoryWatchHandle
    {
        public MemoryRegionSpec Region { get; } = region;

        public ResolvedMemoryRegion ResolvedRegion => default;

        public BitPollResult Poll(Span<BitChange> destination) => default;

        public MemoryUnitPollResult Poll(Span<MemoryUnitChange> destination) => default;

        public WatchSignalResult WaitForSignal(TimeSpan timeout) => default;

        public bool TryReadSnapshot(Span<byte> destination, out int bytesRead)
        {
            bytesRead = 0;
            return false;
        }

        public MemoryWatcherStats GetStats() => default;

        public void Dispose() { }
    }
}
