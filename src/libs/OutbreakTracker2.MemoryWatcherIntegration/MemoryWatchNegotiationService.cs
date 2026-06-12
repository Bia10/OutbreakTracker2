using System.Buffers.Binary;
using System.Diagnostics;
using MemoryWatcher;
using MemoryWatcher.Remote;
using OutbreakTracker2.Extensions;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.PCSX2.Client;

namespace OutbreakTracker2.MemoryWatcherIntegration;

public interface IMemoryWatchNegotiationService
{
    MemoryWatchHostEnvironment InspectHost(MemoryWatcherSettings? settingsOverride = null);

    MemoryWatchCapabilityNegotiationResult Negotiate(
        IGameClient? gameClient,
        MemoryWatcherSettings? settingsOverride = null
    );
}

public sealed class MemoryWatchNegotiationService(
    MemoryWatcherSettings settings,
    IOutbreakTrackerMemoryRegionCatalog regionCatalog
) : IMemoryWatchNegotiationService
{
    private const string DetachedReason = "No running PCSX2 process is attached.";

    private static readonly CapabilityTemplate[] Templates =
    [
        new(
            WatchBackendKind.Snapshot,
            "snapshot-bit-diff",
            MemoryObservationInvasiveness.OutOfProcess,
            MemoryObservationPrecisionClass.SampledFinalValue,
            MemoryObservationLatencyClass.UnknownOrCallerDriven
        ),
        new(
            WatchBackendKind.HashIndexedSnapshot,
            "hash-indexed-snapshot",
            MemoryObservationInvasiveness.OutOfProcess,
            MemoryObservationPrecisionClass.SampledFinalValue,
            MemoryObservationLatencyClass.UnknownOrCallerDriven
        ),
        new(
            WatchBackendKind.SegmentedSnapshot,
            "segmented-snapshot",
            MemoryObservationInvasiveness.OutOfProcess,
            MemoryObservationPrecisionClass.SampledFinalValue,
            MemoryObservationLatencyClass.UnknownOrCallerDriven
        ),
        new(
            WatchBackendKind.DirtyRange,
            "dirty-range-then-diff",
            MemoryObservationInvasiveness.ExecutableHook,
            MemoryObservationPrecisionClass.SignaledFinalValue,
            MemoryObservationLatencyClass.Under1Millisecond
        ),
        new(
            WatchBackendKind.DirtyPage,
            "dirty-page-then-diff",
            MemoryObservationInvasiveness.OperatingSystemHook,
            MemoryObservationPrecisionClass.SignaledFinalValue,
            MemoryObservationLatencyClass.Under1Millisecond
        ),
        new(
            WatchBackendKind.SoftDirty,
            "linux-soft-dirty",
            MemoryObservationInvasiveness.OperatingSystemHook,
            MemoryObservationPrecisionClass.SignaledFinalValue,
            MemoryObservationLatencyClass.Under1Millisecond
        ),
        new(
            WatchBackendKind.PageFault,
            "page-fault-then-diff",
            MemoryObservationInvasiveness.OperatingSystemHook,
            MemoryObservationPrecisionClass.SignaledFinalValue,
            MemoryObservationLatencyClass.Under1Millisecond
        ),
        new(
            WatchBackendKind.HardwareWatchpoint,
            "hardware-watchpoint",
            MemoryObservationInvasiveness.OperatingSystemHook,
            MemoryObservationPrecisionClass.SignaledFinalValue,
            MemoryObservationLatencyClass.Under1Millisecond
        ),
        new(
            WatchBackendKind.NativeAgent,
            "native-agent",
            MemoryObservationInvasiveness.ExecutableHook,
            MemoryObservationPrecisionClass.SignaledFinalValue,
            MemoryObservationLatencyClass.Under1Millisecond
        ),
    ];

    private readonly MemoryWatcherSettings _settings = settings;
    private readonly IOutbreakTrackerMemoryRegionCatalog _regionCatalog = regionCatalog;

    public MemoryWatchHostEnvironment InspectHost(MemoryWatcherSettings? settingsOverride = null) =>
        CreateNegotiator(settingsOverride ?? _settings).InspectHost();

    public MemoryWatchCapabilityNegotiationResult Negotiate(
        IGameClient? gameClient,
        MemoryWatcherSettings? settingsOverride = null
    )
    {
        MemoryWatcherSettings effectiveSettings = settingsOverride ?? _settings;
        IMemoryWatchCapabilityNegotiator capabilityNegotiator = CreateNegotiator(effectiveSettings);
        IMemoryWatchSessionFactory sessionFactory = CreateSessionFactory(effectiveSettings);
        MemoryWatchHostEnvironment host = capabilityNegotiator.InspectHost();
        if (gameClient?.Process is not Process process)
        {
            return CreateDetachedResult(host);
        }

        string moduleName = process.GetSafeName() ?? process.ProcessName;
        IReadOnlyList<int> hardwareThreadIds = process.GetSafeThreadIds();
        bool probeResolved = TryResolveNegotiationContext(
            gameClient,
            process.Id,
            moduleName,
            sessionFactory,
            out NegotiationProbeContext probeContext
        );
        MemoryWatchCapabilityNegotiationResult groupedResult = capabilityNegotiator.Negotiate(
            new MemoryWatchCapabilityNegotiationRequest
            {
                ProcessId = process.Id,
                Region = probeResolved ? probeContext.GroupedProbeRegion : null,
                SessionOptions = effectiveSettings.ToSessionOptions(hardwareThreadIds),
                HardwareThreadIds = hardwareThreadIds,
            }
        );

        if (!probeResolved || probeContext.ResolvedRegions.Count == 0)
        {
            return groupedResult;
        }

        HashSet<WatchBackendKind> ot2ReadyBackends = BuildOt2ReadyBackends(probeContext);
        Dictionary<WatchBackendKind, MemoryWatchNegotiatedCapability> backendProbes = new();
        foreach (CapabilityTemplate template in Templates)
        {
            if (!RequiresBackendProbe(template.Backend))
            {
                continue;
            }

            try
            {
                MemoryRegionSpec backendProbe = MemoryWatchProbePlanner.CreateBackendProbe(
                    template.Backend,
                    probeContext.EEmemBaseAddress,
                    probeContext.ResolvedRegions
                );
                MemoryWatchSessionOptions probeSessionOptions =
                    MemoryWatchProbePlanner.CreateBackendProbeSessionOptions(
                        effectiveSettings,
                        template.Backend,
                        hardwareThreadIds
                    );
                MemoryWatchCapabilityNegotiationResult backendResult = capabilityNegotiator.Negotiate(
                    new MemoryWatchCapabilityNegotiationRequest
                    {
                        ProcessId = process.Id,
                        Region = backendProbe,
                        SessionOptions = probeSessionOptions,
                        HardwareThreadIds = hardwareThreadIds,
                    }
                );

                MemoryWatchNegotiatedCapability? capability = backendResult.Capabilities.FirstOrDefault(candidate =>
                    candidate.Backend == template.Backend
                );
                if (capability is not null)
                {
                    backendProbes[template.Backend] = ValidateBackendProbeCapability(
                        sessionFactory,
                        process.Id,
                        backendProbe,
                        probeSessionOptions,
                        capability
                    );
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        return MemoryWatchProbePlanner.MergeWithBackendProbes(groupedResult, backendProbes, ot2ReadyBackends);
    }

    private static bool RequiresBackendProbe(WatchBackendKind backend) =>
        backend
            is not WatchBackendKind.Snapshot
                and not WatchBackendKind.HashIndexedSnapshot
                and not WatchBackendKind.SegmentedSnapshot;

    private static IMemoryWatchCapabilityNegotiator CreateNegotiator(MemoryWatcherSettings settings) =>
        new NativeMemoryWatchCapabilityNegotiator(settings.NativeLibraryPath);

    private static IMemoryWatchSessionFactory CreateSessionFactory(MemoryWatcherSettings settings) =>
        new NativeMemoryWatchSessionFactory(settings.NativeLibraryPath);

    private bool TryResolveNegotiationContext(
        IGameClient gameClient,
        int processId,
        string moduleName,
        IMemoryWatchSessionFactory sessionFactory,
        out NegotiationProbeContext context
    )
    {
        if (
            !MemoryWatcherEEmemSpecFactory.TryCreateEEmemExportPointerSpec(
                gameClient,
                moduleName,
                out MemoryRegionSpec exportSpec
            )
        )
        {
            context = default;
            return false;
        }

        try
        {
            using IMemoryWatchSession session = sessionFactory.Open(processId, CreateProbeReadSessionOptions());
            using IMemoryWatchHandle handle = session.CreateWatch(exportSpec);

            if (!TryReadEEmemPointer(handle, out nint eememBaseAddress) || eememBaseAddress == nint.Zero)
            {
                context = new NegotiationProbeContext(exportSpec, nint.Zero, GameFile.Unknown, []);
                return true;
            }

            MemoryWatchGameFileDetector.TryDetectActiveGameFile(session, eememBaseAddress, out GameFile activeGameFile);
            IReadOnlyList<OutbreakTrackerMemoryRegionDefinition> regions = _regionCatalog.CreateRegions(
                eememBaseAddress
            );
            if (regions.Count == 0)
            {
                context = new NegotiationProbeContext(exportSpec, eememBaseAddress, activeGameFile, regions);
                return true;
            }

            MemoryRegionSpec groupedProbe = MemoryWatchProbePlanner.CreateGroupedOt2Probe(regions);
            context = new NegotiationProbeContext(groupedProbe, eememBaseAddress, activeGameFile, regions);
            return true;
        }
        catch (Exception ex)
            when (ex is InvalidOperationException or IOException or UnauthorizedAccessException or NotSupportedException
            )
        {
            context = new NegotiationProbeContext(exportSpec, nint.Zero, GameFile.Unknown, []);
            return true;
        }
    }

    private static MemoryWatchNegotiatedCapability ValidateBackendProbeCapability(
        IMemoryWatchSessionFactory sessionFactory,
        int processId,
        MemoryRegionSpec backendProbe,
        MemoryWatchSessionOptions sessionOptions,
        MemoryWatchNegotiatedCapability capability
    )
    {
        if (!capability.CurrentRequestAvailable)
        {
            return capability;
        }

        try
        {
            using IMemoryWatchSession session = sessionFactory.Open(processId, sessionOptions);
            using IMemoryWatchHandle handle = session.CreateWatch(backendProbe);
            return capability;
        }
        catch (Exception ex)
            when (ex is InvalidOperationException or IOException or UnauthorizedAccessException or NotSupportedException
            )
        {
            string reason = $"Live probe creation failed: {ex.Message}";
            return capability with
            {
                EnvironmentSupport =
                    capability.EnvironmentSupport == MemoryCapabilitySupportLevel.Supported
                        ? MemoryCapabilitySupportLevel.Conditional
                        : capability.EnvironmentSupport,
                EnvironmentConstraintKind = MemoryCapabilityConstraintKind.Unknown,
                EnvironmentSupportReason = reason,
                CurrentRequestAvailable = false,
                CurrentRequestConstraintKind = MemoryCapabilityConstraintKind.Unknown,
                CurrentRequestReason = reason,
            };
        }
    }

    private static HashSet<WatchBackendKind> BuildOt2ReadyBackends(NegotiationProbeContext context)
    {
        HashSet<WatchBackendKind> readyBackends = [];
        foreach (WatchBackendKind backend in Enum.GetValues<WatchBackendKind>())
        {
            if (
                MemoryWatchActivityPlanFactory.TryCreateActivityPlan(
                    backend,
                    context.EEmemBaseAddress,
                    context.ActiveGameFile,
                    context.ResolvedRegions,
                    out _
                )
            )
            {
                readyBackends.Add(backend);
            }
        }

        return readyBackends;
    }

    private static MemoryWatchCapabilityNegotiationResult CreateDetachedResult(MemoryWatchHostEnvironment host)
    {
        MemoryWatchTargetEnvironment target = new()
        {
            ProcessId = 0,
            ProcessFound = false,
            SessionOpened = false,
            SessionFailureReason = DetachedReason,
        };

        MemoryWatchNegotiatedCapability[] capabilities = new MemoryWatchNegotiatedCapability[Templates.Length];
        for (int i = 0; i < Templates.Length; i++)
        {
            CapabilityTemplate template = Templates[i];
            capabilities[i] = new MemoryWatchNegotiatedCapability
            {
                Backend = template.Backend,
                BackendName = template.BackendName,
                Invasiveness = template.Invasiveness,
                PrecisionClass = template.PrecisionClass,
                LatencyClass = template.LatencyClass,
                EnvironmentSupport = EvaluateDetachedSupport(template.Backend, host),
                EnvironmentConstraintKind = EvaluateDetachedConstraintKind(template.Backend, host),
                EnvironmentSupportReason = DescribeDetachedEnvironment(template.Backend, host),
                CurrentCapability = null,
                CurrentRequestAvailable = false,
                CurrentRequestConstraintKind = MemoryCapabilityConstraintKind.TargetProcessMissing,
                CurrentRequestReason = DetachedReason,
            };
        }

        return new MemoryWatchCapabilityNegotiationResult
        {
            Host = host,
            Target = target,
            Capabilities = capabilities,
        };
    }

    private static MemoryWatchSessionOptions CreateProbeReadSessionOptions() =>
        new()
        {
            PreferredBackend = WatchBackendKind.Auto,
            PreferredPrecision = WatchPrecision.SnapshotBitExact,
            AllowFallback = true,
        };

    private static MemoryCapabilitySupportLevel EvaluateDetachedSupport(
        WatchBackendKind backend,
        MemoryWatchHostEnvironment host
    )
    {
        return backend switch
        {
            WatchBackendKind.Snapshot or WatchBackendKind.HashIndexedSnapshot or WatchBackendKind.SegmentedSnapshot =>
                MemoryCapabilitySupportLevel.Conditional,
            WatchBackendKind.DirtyRange => MemoryCapabilitySupportLevel.Conditional,
            WatchBackendKind.DirtyPage => MemoryCapabilitySupportLevel.Conditional,
            WatchBackendKind.SoftDirty => host.SupportsSoftDirtyTracking
                ? MemoryCapabilitySupportLevel.Conditional
                : MemoryCapabilitySupportLevel.Unsupported,
            WatchBackendKind.PageFault => OperatingSystem.IsWindows()
                ? MemoryCapabilitySupportLevel.Conditional
                : MemoryCapabilitySupportLevel.Unsupported,
            WatchBackendKind.HardwareWatchpoint => host.SupportsDebuggerMediatedHardwareWatchpoints
                ? MemoryCapabilitySupportLevel.Conditional
                : MemoryCapabilitySupportLevel.Unsupported,
            WatchBackendKind.NativeAgent => MemoryCapabilitySupportLevel.Conditional,
            _ => MemoryCapabilitySupportLevel.Unsupported,
        };
    }

    private static MemoryCapabilityConstraintKind EvaluateDetachedConstraintKind(
        WatchBackendKind backend,
        MemoryWatchHostEnvironment host
    )
    {
        return backend switch
        {
            WatchBackendKind.Snapshot or WatchBackendKind.HashIndexedSnapshot or WatchBackendKind.SegmentedSnapshot =>
                MemoryCapabilityConstraintKind.TargetProcessMissing,
            WatchBackendKind.DirtyRange => MemoryCapabilityConstraintKind.RequiresCooperativeProducer,
            WatchBackendKind.DirtyPage => MemoryCapabilityConstraintKind.RequiresPageTracker,
            WatchBackendKind.SoftDirty when !host.SupportsSoftDirtyTracking =>
                MemoryCapabilityConstraintKind.UnsupportedHostPlatform,
            WatchBackendKind.SoftDirty => MemoryCapabilityConstraintKind.TargetProcessMissing,
            WatchBackendKind.PageFault when !OperatingSystem.IsWindows() =>
                MemoryCapabilityConstraintKind.UnsupportedHostPlatform,
            WatchBackendKind.PageFault => MemoryCapabilityConstraintKind.TargetProcessMissing,
            WatchBackendKind.HardwareWatchpoint when !host.SupportsDebuggerMediatedHardwareWatchpoints =>
                MemoryCapabilityConstraintKind.UnsupportedHostPlatform,
            WatchBackendKind.HardwareWatchpoint => MemoryCapabilityConstraintKind.TargetProcessMissing,
            WatchBackendKind.NativeAgent => MemoryCapabilityConstraintKind.RequiresAgent,
            _ => MemoryCapabilityConstraintKind.Unknown,
        };
    }

    private static string DescribeDetachedEnvironment(WatchBackendKind backend, MemoryWatchHostEnvironment host)
    {
        return backend switch
        {
            WatchBackendKind.HashIndexedSnapshot =>
                "Attach to a running PCSX2 process to validate hash-indexed grouped snapshot reads.",
            WatchBackendKind.SegmentedSnapshot =>
                "Attach to a running PCSX2 process to validate segmented grouped snapshot reads.",
            WatchBackendKind.Snapshot => "Attach to a running PCSX2 process to validate the plain external reader.",
            WatchBackendKind.DirtyRange =>
                "Dirty-range observation requires a cooperative producer or in-process helper that can publish changed spans.",
            WatchBackendKind.DirtyPage =>
                "Dirty-page observation needs page markers or another OS-mediated producer path beyond a plain external reader.",
            WatchBackendKind.SoftDirty when !host.SupportsSoftDirtyTracking =>
                "Linux soft-dirty tracking is only available on Linux hosts.",
            WatchBackendKind.SoftDirty =>
                "Attach to a running Linux PCSX2 process to validate /proc-based soft-dirty access.",
            WatchBackendKind.PageFault when !OperatingSystem.IsWindows() =>
                "Debugger-mediated PAGE_GUARD page-fault observation is currently implemented for Windows hosts.",
            WatchBackendKind.PageFault =>
                "Attach to a running Windows PCSX2 process and enable intrusive backends to validate debugger-mediated PAGE_GUARD wakeups.",
            WatchBackendKind.HardwareWatchpoint when !host.SupportsDebuggerMediatedHardwareWatchpoints =>
                "Debugger/perf mediated hardware watchpoints are currently implemented for 64-bit Windows and 64-bit Linux hosts.",
            WatchBackendKind.HardwareWatchpoint =>
                "Attach to a running PCSX2 process and provide explicit target thread ids to fully negotiate per-thread watchpoint arming.",
            WatchBackendKind.NativeAgent =>
                "Native-agent observation requires loading a cooperative helper inside the target process.",
            _ => "No environment evaluator is defined for this backend.",
        };
    }

    private static bool TryReadEEmemPointer(IMemoryWatchHandle exportHandle, out nint eememBaseAddress)
    {
        Span<byte> snapshot = stackalloc byte[sizeof(long)];
        if (!exportHandle.TryReadSnapshot(snapshot, out int bytesRead) || bytesRead < sizeof(long))
        {
            eememBaseAddress = nint.Zero;
            return false;
        }

        eememBaseAddress = checked((nint)BinaryPrimitives.ReadUInt64LittleEndian(snapshot));
        return true;
    }

    private sealed record CapabilityTemplate(
        WatchBackendKind Backend,
        string BackendName,
        MemoryObservationInvasiveness Invasiveness,
        MemoryObservationPrecisionClass PrecisionClass,
        MemoryObservationLatencyClass LatencyClass
    );

    private readonly record struct NegotiationProbeContext(
        MemoryRegionSpec GroupedProbeRegion,
        nint EEmemBaseAddress,
        GameFile ActiveGameFile,
        IReadOnlyList<OutbreakTrackerMemoryRegionDefinition> ResolvedRegions
    );
}
