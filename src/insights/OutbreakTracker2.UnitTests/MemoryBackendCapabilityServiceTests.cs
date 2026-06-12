using MemoryWatcher;
using MemoryWatcher.Remote;
using OutbreakTracker2.Application.Services.Capabilities;
using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.MemoryWatcherIntegration;
using R3;

namespace OutbreakTracker2.UnitTests;

public sealed class MemoryBackendCapabilityServiceTests
{
    [Test]
    public async Task Inspect_UsesSnapshotStrategyAndConfiguredPollingLatency_WhenMemoryWatcherIsCurrentBackend()
    {
        using FakeAppSettingsService settingsService = new(
            new OutbreakTrackerSettings
            {
                DataManager = new DataManagerSettings { FastUpdateIntervalMs = 250, SlowUpdateIntervalMs = 500 },
                MemoryWatcher = new MemoryWatcherSettings { Backend = MemoryBackendMode.MemoryWatcher },
            }
        );

        MemoryWatchCapabilityNegotiationResult negotiation = new()
        {
            Host = new MemoryWatchHostEnvironment
            {
                OperatingSystem = "TestOS",
                RuntimeDescription = ".NET Test",
                ProcessArchitecture = System.Runtime.InteropServices.Architecture.X64,
                Is64BitProcess = true,
                UserName = "tester",
                IsElevatedUser = false,
                SupportsPackagedRemoteAot = true,
                SupportsSoftDirtyTracking = false,
                SupportsDebuggerMediatedHardwareWatchpoints = true,
            },
            Target = new MemoryWatchTargetEnvironment
            {
                ProcessId = 123,
                ProcessName = "pcsx2",
                ProcessFound = true,
                SessionOpened = true,
            },
            Capabilities =
            [
                new MemoryWatchNegotiatedCapability
                {
                    Backend = WatchBackendKind.Snapshot,
                    BackendName = "snapshot-bit-diff",
                    Invasiveness = MemoryObservationInvasiveness.OutOfProcess,
                    PrecisionClass = MemoryObservationPrecisionClass.SampledFinalValue,
                    LatencyClass = MemoryObservationLatencyClass.UnknownOrCallerDriven,
                    EnvironmentSupport = MemoryCapabilitySupportLevel.Supported,
                    EnvironmentConstraintKind = MemoryCapabilityConstraintKind.None,
                    CurrentRequestAvailable = true,
                    CurrentRequestConstraintKind = MemoryCapabilityConstraintKind.None,
                },
                new MemoryWatchNegotiatedCapability
                {
                    Backend = WatchBackendKind.HardwareWatchpoint,
                    BackendName = "hardware-watchpoint",
                    Invasiveness = MemoryObservationInvasiveness.OperatingSystemHook,
                    PrecisionClass = MemoryObservationPrecisionClass.SignaledFinalValue,
                    LatencyClass = MemoryObservationLatencyClass.Under1Millisecond,
                    EnvironmentSupport = MemoryCapabilitySupportLevel.Supported,
                    EnvironmentConstraintKind = MemoryCapabilityConstraintKind.None,
                    CurrentRequestAvailable = false,
                    CurrentRequestConstraintKind = MemoryCapabilityConstraintKind.MissingHardwareThreadIds,
                    CurrentRequestReason = "Thread ids were not supplied.",
                },
            ],
        };

        MemoryBackendCapabilityService service = new(
            new FakeMemoryWatchNegotiationService(negotiation),
            settingsService
        );

        MemoryBackendCapabilityReport report = service.Inspect(gameClient: null);

        MemoryBackendCapability legacy = report.Backends.Single(static backend =>
            backend.Mode == MemoryBackendMode.Legacy
        );
        MemoryBackendCapability memoryWatcher = report.Backends.Single(static backend =>
            backend.Mode == MemoryBackendMode.MemoryWatcher
        );

        await Assert.That(legacy.Support).IsEqualTo(MemoryCapabilitySupportLevel.Conditional);
        await Assert.That(legacy.LatencyClass).IsEqualTo(MemoryObservationLatencyClass.OverOrEqual100Milliseconds);

        await Assert.That(memoryWatcher.Support).IsEqualTo(MemoryCapabilitySupportLevel.Supported);
        await Assert.That(memoryWatcher.IsConfiguredDefault).IsTrue();
        await Assert.That(memoryWatcher.Invasiveness).IsEqualTo(MemoryObservationInvasiveness.OutOfProcess);
        await Assert.That(memoryWatcher.PrecisionClass).IsEqualTo(MemoryObservationPrecisionClass.SampledFinalValue);
        await Assert
            .That(memoryWatcher.LatencyClass)
            .IsEqualTo(MemoryObservationLatencyClass.OverOrEqual100Milliseconds);
    }

    private sealed class FakeMemoryWatchNegotiationService(MemoryWatchCapabilityNegotiationResult result)
        : IMemoryWatchNegotiationService
    {
        public MemoryWatchHostEnvironment InspectHost(MemoryWatcherSettings? settingsOverride = null) => result.Host;

        public MemoryWatchCapabilityNegotiationResult Negotiate(
            OutbreakTracker2.PCSX2.Client.IGameClient? gameClient,
            MemoryWatcherSettings? settingsOverride = null
        ) => result;
    }

    private sealed class FakeAppSettingsService(OutbreakTrackerSettings settings) : IAppSettingsService
    {
        private readonly ReactiveProperty<OutbreakTrackerSettings> _settings = new(settings);

        public string UserSettingsPath => string.Empty;

        public OutbreakTrackerSettings Current => _settings.Value;

        public Observable<OutbreakTrackerSettings> SettingsObservable => _settings;

        public ValueTask SaveAsync(OutbreakTrackerSettings settings, CancellationToken cancellationToken = default)
        {
            _settings.Value = settings;
            return ValueTask.CompletedTask;
        }

        public ValueTask ExportAsync(Stream destination, CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;

        public ValueTask<OutbreakTrackerSettings> ImportAsync(
            Stream source,
            CancellationToken cancellationToken = default
        ) => ValueTask.FromResult(Current);

        public ValueTask<OutbreakTrackerSettings> ResetToDefaultsAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Current);

        public void Dispose() => _settings.Dispose();
    }
}
