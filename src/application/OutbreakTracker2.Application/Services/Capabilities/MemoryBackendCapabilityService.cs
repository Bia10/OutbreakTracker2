using MemoryWatcher;
using MemoryWatcher.Remote;
using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.MemoryWatcherIntegration;
using OutbreakTracker2.PCSX2.Client;

namespace OutbreakTracker2.Application.Services.Capabilities;

public sealed class MemoryBackendCapabilityService(
    IMemoryWatchNegotiationService memoryWatchNegotiationService,
    IAppSettingsService settingsService
) : IMemoryBackendCapabilityService
{
    private readonly IMemoryWatchNegotiationService _memoryWatchNegotiationService = memoryWatchNegotiationService;
    private readonly IAppSettingsService _settingsService = settingsService;

    public MemoryBackendCapabilityReport Inspect(
        IGameClient? gameClient,
        OutbreakTrackerSettings? settingsOverride = null
    )
    {
        OutbreakTrackerSettings settings = settingsOverride ?? _settingsService.Current;
        MemoryWatchCapabilityNegotiationResult memoryWatcher = _memoryWatchNegotiationService.Negotiate(
            gameClient,
            settings.MemoryWatcher
        );
        MemoryBackendCapability legacy = BuildLegacyCapability(settings, gameClient);
        MemoryBackendCapability negotiated = BuildMemoryWatcherCapability(settings, memoryWatcher);

        return new MemoryBackendCapabilityReport
        {
            Host = memoryWatcher.Host,
            MemoryWatcher = memoryWatcher,
            Backends = [legacy, negotiated],
        };
    }

    private static MemoryBackendCapability BuildLegacyCapability(
        OutbreakTrackerSettings settings,
        IGameClient? gameClient
    )
    {
        bool processAttached = gameClient?.Process is not null;
        return new MemoryBackendCapability
        {
            Mode = MemoryBackendMode.Legacy,
            Support = processAttached
                ? MemoryCapabilitySupportLevel.Supported
                : MemoryCapabilitySupportLevel.Conditional,
            Invasiveness = MemoryObservationInvasiveness.OutOfProcess,
            PrecisionClass = MemoryObservationPrecisionClass.SampledFinalValue,
            LatencyClass = ClassifyInterval(settings.DataManager.FastUpdateIntervalMs),
            IsConfiguredDefault = settings.MemoryWatcher.Backend == MemoryBackendMode.Legacy,
            Reason = processAttached
                ? null
                : "Attach to a running PCSX2 process to validate the legacy out-of-process reader.",
        };
    }

    private static MemoryBackendCapability BuildMemoryWatcherCapability(
        OutbreakTrackerSettings settings,
        MemoryWatchCapabilityNegotiationResult report
    )
    {
        MemoryWatchNegotiatedCapability strategy =
            SelectRequestedStrategy(settings.MemoryWatcher, report.Capabilities)
            ?? SelectConfiguredStrategy(report.Capabilities);
        MemoryCapabilitySupportLevel support = strategy.CurrentRequestAvailable
            ? MemoryCapabilitySupportLevel.Supported
            : strategy.EnvironmentSupport;
        string? reason = strategy.CurrentRequestAvailable
            ? null
            : strategy.CurrentRequestReason ?? strategy.EnvironmentSupportReason;

        MemoryObservationLatencyClass latency =
            strategy.LatencyClass == MemoryObservationLatencyClass.UnknownOrCallerDriven
                ? ClassifyInterval(settings.DataManager.FastUpdateIntervalMs)
                : strategy.LatencyClass;

        return new MemoryBackendCapability
        {
            Mode = MemoryBackendMode.MemoryWatcher,
            Support = support,
            Invasiveness = strategy.Invasiveness,
            PrecisionClass = strategy.PrecisionClass,
            LatencyClass = latency,
            IsConfiguredDefault = settings.MemoryWatcher.Backend == MemoryBackendMode.MemoryWatcher,
            Reason = reason,
        };
    }

    private static MemoryWatchNegotiatedCapability? SelectRequestedStrategy(
        MemoryWatcherSettings settings,
        IReadOnlyList<MemoryWatchNegotiatedCapability> strategies
    )
    {
        if (strategies.Count == 0)
        {
            return null;
        }

        if (settings.PreferredBackend != WatchBackendKind.Auto)
        {
            MemoryWatchNegotiatedCapability? exact = strategies.FirstOrDefault(strategy =>
                strategy.Backend == settings.PreferredBackend
            );
            if (exact is not null)
            {
                return exact;
            }

            return settings.AllowFallback ? SelectConfiguredStrategy(strategies) : null;
        }

        MemoryWatchNegotiatedCapability? precisionMatch = strategies
            .Where(strategy => GetEffectivePrecision(strategy) == settings.PreferredPrecision)
            .OrderByDescending(static strategy => strategy.CurrentRequestAvailable)
            .ThenByDescending(static strategy => strategy.EnvironmentSupport == MemoryCapabilitySupportLevel.Supported)
            .ThenBy(static strategy => GetInvasivenessRank(strategy.Invasiveness))
            .ThenByDescending(static strategy => GetLatencyRank(strategy.LatencyClass))
            .FirstOrDefault();

        return precisionMatch ?? (settings.AllowFallback ? SelectConfiguredStrategy(strategies) : null);
    }

    private static MemoryWatchNegotiatedCapability SelectConfiguredStrategy(
        IReadOnlyList<MemoryWatchNegotiatedCapability> strategies
    )
    {
        MemoryWatchNegotiatedCapability? current = strategies
            .Where(static strategy => strategy.CurrentRequestAvailable)
            .OrderBy(static strategy => GetInvasivenessRank(strategy.Invasiveness))
            .ThenByDescending(static strategy => GetPrecisionRank(strategy.PrecisionClass))
            .ThenByDescending(static strategy => GetLatencyRank(strategy.LatencyClass))
            .FirstOrDefault();
        if (current is not null)
        {
            return current;
        }

        MemoryWatchNegotiatedCapability? supported = strategies
            .Where(static strategy => strategy.EnvironmentSupport == MemoryCapabilitySupportLevel.Supported)
            .OrderBy(static strategy => GetInvasivenessRank(strategy.Invasiveness))
            .ThenByDescending(static strategy => GetPrecisionRank(strategy.PrecisionClass))
            .ThenByDescending(static strategy => GetLatencyRank(strategy.LatencyClass))
            .FirstOrDefault();
        if (supported is not null)
        {
            return supported;
        }

        MemoryWatchNegotiatedCapability? conditional = strategies
            .Where(static strategy => strategy.EnvironmentSupport == MemoryCapabilitySupportLevel.Conditional)
            .OrderBy(static strategy => GetInvasivenessRank(strategy.Invasiveness))
            .ThenByDescending(static strategy => GetPrecisionRank(strategy.PrecisionClass))
            .ThenByDescending(static strategy => GetLatencyRank(strategy.LatencyClass))
            .FirstOrDefault();
        if (conditional is not null)
        {
            return conditional;
        }

        return strategies.First();
    }

    private static MemoryObservationLatencyClass ClassifyInterval(int intervalMs)
    {
        if (intervalMs <= 1)
        {
            return MemoryObservationLatencyClass.Under1Millisecond;
        }

        if (intervalMs < 100)
        {
            return MemoryObservationLatencyClass.Under100Milliseconds;
        }

        return MemoryObservationLatencyClass.OverOrEqual100Milliseconds;
    }

    private static int GetInvasivenessRank(MemoryObservationInvasiveness invasiveness)
    {
        return invasiveness switch
        {
            MemoryObservationInvasiveness.OutOfProcess => 0,
            MemoryObservationInvasiveness.OperatingSystemHook => 1,
            MemoryObservationInvasiveness.ExecutableHook => 2,
            MemoryObservationInvasiveness.KernelHook => 3,
            _ => 4,
        };
    }

    private static int GetPrecisionRank(MemoryObservationPrecisionClass precision)
    {
        return precision switch
        {
            MemoryObservationPrecisionClass.TransientEdgeExact => 2,
            MemoryObservationPrecisionClass.SignaledFinalValue => 1,
            _ => 0,
        };
    }

    private static int GetLatencyRank(MemoryObservationLatencyClass latency)
    {
        return latency switch
        {
            MemoryObservationLatencyClass.Under50Nanoseconds => 5,
            MemoryObservationLatencyClass.Under100Nanoseconds => 4,
            MemoryObservationLatencyClass.Under1Millisecond => 3,
            MemoryObservationLatencyClass.Under100Milliseconds => 2,
            MemoryObservationLatencyClass.OverOrEqual100Milliseconds => 1,
            _ => 0,
        };
    }

    private static WatchPrecision GetEffectivePrecision(MemoryWatchNegotiatedCapability strategy) =>
        strategy.CurrentCapability?.Precision
        ?? strategy.Backend switch
        {
            WatchBackendKind.Snapshot or WatchBackendKind.HashIndexedSnapshot or WatchBackendKind.SegmentedSnapshot =>
                WatchPrecision.SnapshotBitExact,
            WatchBackendKind.DirtyPage => WatchPrecision.DirtyPageThenBitDiff,
            WatchBackendKind.SoftDirty => WatchPrecision.SoftDirtyThenBitDiff,
            WatchBackendKind.PageFault => WatchPrecision.PageFaultThenBitDiff,
            WatchBackendKind.HardwareWatchpoint => WatchPrecision.HardwareAddressExact,
            WatchBackendKind.NativeAgent or WatchBackendKind.DirtyRange => WatchPrecision.DirtyRangeThenBitDiff,
            _ => WatchPrecision.SnapshotBitExact,
        };
}
