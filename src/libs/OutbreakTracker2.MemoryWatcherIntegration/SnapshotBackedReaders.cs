using System.Buffers;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using MemoryWatcher;
using MemoryWatcher.Remote;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Extensions;
using OutbreakTracker2.LinuxInterop;
using OutbreakTracker2.Memory.SafeMemory;
using OutbreakTracker2.Memory.String;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.PCSX2.Client;
using OutbreakTracker2.PCSX2.EEmem;
using OutbreakTracker2.WinInterop;

namespace OutbreakTracker2.MemoryWatcherIntegration;

public sealed class OutbreakTrackerMemoryRegionCatalog : IOutbreakTrackerMemoryRegionCatalog
{
    private const int FileOneDoorCount = GameConstants.MaxDoors - 9;
    private const int ObservedPlayerBytesFileOne = (int)FileOnePtrs.InventoryOffset + 9;
    private const int ObservedPlayerBytesFileTwo = (int)FileTwoPtrs.InventoryOffset + 9;
    private const int ObservedPickupBytes = (int)FileOnePtrs.PickupOffset + sizeof(short);
    private const int ObservedLobbyRoomPlayerBytesFileOne = (int)FileOnePtrs.LobbyRoomPlayerNpcTypeOffset + 1;
    private const int ObservedLobbyRoomPlayerBytesFileTwo = (int)FileTwoPtrs.LobbyRoomPlayerEnabledOffset + 1;
    private const int ObservedEnemyListEntryBytes = (int)0x46;
    private const int DeadInventoryBytes = GameConstants.MaxPlayers * 8;
    private const int VirusMaxBytes = GameConstants.MaxCharacterData * sizeof(int);
    private static readonly RelativeRegionDefinition[] RelativeRegions = CreateRelativeRegions();

    public IReadOnlyList<OutbreakTrackerMemoryRegionDefinition> CreateRegions(nint eememBaseAddress)
    {
        OutbreakTrackerMemoryRegionDefinition[] regions = new OutbreakTrackerMemoryRegionDefinition[
            RelativeRegions.Length
        ];
        for (int i = 0; i < RelativeRegions.Length; i++)
        {
            RelativeRegionDefinition region = RelativeRegions[i];
            regions[i] = new OutbreakTrackerMemoryRegionDefinition(
                region.Name,
                eememBaseAddress + region.StartOffset,
                region.ByteLength,
                region.Domains
            );
        }

        return regions;
    }

    private static RelativeRegionDefinition Region(
        string name,
        RelativeMemoryRange range,
        OutbreakTrackerMemoryDomains domains
    ) => new(name, range.Start, range.ByteLength, domains);

    private static RelativeRegionDefinition[] CreateRelativeRegions()
    {
        return
        [
            Region(
                "ScenarioDiscSignatureFileOne",
                Bytes(FileOnePtrs.DiscStart, 1),
                OutbreakTrackerMemoryDomains.Scenario
            ),
            Region(
                "ScenarioDiscSignatureFileTwo",
                Bytes(FileTwoPtrs.DiscStart, 1),
                OutbreakTrackerMemoryDomains.Scenario
            ),
            Region(
                "ScenarioRandomsFileOne",
                Union(
                    Bytes(FileOnePtrs.ItemRandom, 1),
                    Bytes(FileOnePtrs.ItemRandom2, 1),
                    Bytes(FileOnePtrs.PuzzleRandom, 1),
                    Bytes(FileOnePtrs.InGamePlayerNumber, 1)
                ),
                OutbreakTrackerMemoryDomains.Scenario
            ),
            Region(
                "ScenarioRandomsFileTwo",
                Union(
                    Bytes(FileTwoPtrs.ItemRandom, 1),
                    Bytes(FileTwoPtrs.ItemRandom2, 1),
                    Bytes(FileTwoPtrs.PuzzleRandom, 1),
                    Bytes(FileTwoPtrs.IngamePlayerNumber, 1)
                ),
                OutbreakTrackerMemoryDomains.Scenario
            ),
            Region(
                "ScenarioIdsFileOne",
                Bytes(FileOnePtrs.InGameScenarioId, sizeof(short)),
                OutbreakTrackerMemoryDomains.Scenario
            ),
            Region(
                "ScenarioIdsFileTwo",
                Bytes(FileTwoPtrs.InGameScenarioId, sizeof(short)),
                OutbreakTrackerMemoryDomains.Scenario
            ),
            Region(
                "ScenarioPickupTableFileOne",
                ObservedArray(
                    FileOnePtrs.PickupSpaceStart,
                    FileOnePtrs.PickupStructSize,
                    GameConstants.MaxItems - 1,
                    ObservedPickupBytes
                ),
                OutbreakTrackerMemoryDomains.Scenario
            ),
            Region(
                "ScenarioPickupTableFileTwo",
                ObservedArray(
                    FileTwoPtrs.PickupSpaceStart,
                    FileTwoPtrs.PickupStructSize,
                    GameConstants.MaxItems - 1,
                    ObservedPickupBytes
                ),
                OutbreakTrackerMemoryDomains.Scenario
            ),
            Region(
                "ScenarioWildThingsGateFileTwo",
                Bytes(FileTwoPtrs.WTGateMHp, sizeof(int)),
                OutbreakTrackerMemoryDomains.Scenario
            ),
            Region(
                "ScenarioPassStateFileOne",
                Union(
                    Bytes(FileOnePtrs.Pass1, 1),
                    Bytes(FileOnePtrs.Pass2, 1),
                    Bytes(FileOnePtrs.Pass3, 1),
                    Bytes(FileOnePtrs.Pass4, sizeof(short)),
                    Bytes(FileOnePtrs.Pass5, 1),
                    Bytes(FileOnePtrs.Pass6, 1)
                ),
                OutbreakTrackerMemoryDomains.Scenario
            ),
            Region(
                "ScenarioLifecycleFileOne",
                Union(
                    Bytes(FileOnePtrs.InGameFrameCounter, sizeof(int)),
                    Bytes(FileOnePtrs.ScenarioStatus, 1),
                    Bytes(FileOnePtrs.Difficulty, 1)
                ),
                OutbreakTrackerMemoryDomains.Scenario
            ),
            Region(
                "ScenarioPassStateFileTwo",
                Union(
                    Bytes(FileTwoPtrs.PassDesperateTimes3, 1),
                    Bytes(FileTwoPtrs.PassWildThings, 1),
                    Bytes(FileTwoPtrs.PassDesperateTimes, sizeof(short)),
                    Bytes(FileTwoPtrs.PassDesperateTimes2, 1),
                    Bytes(FileTwoPtrs.PassUnderBelly1, sizeof(short)),
                    Bytes(FileTwoPtrs.PassUnderBelly2, 1),
                    Bytes(FileTwoPtrs.PassUnderBelly3, 1),
                    Bytes(FileTwoPtrs.DesperateTimesGasFlag, sizeof(int))
                ),
                OutbreakTrackerMemoryDomains.Scenario
            ),
            Region(
                "ScenarioMetricsFileTwo",
                Union(
                    Bytes(FileTwoPtrs.Pass4, sizeof(short)),
                    Bytes(FileTwoPtrs.Coin, sizeof(short) * 4),
                    Bytes(FileTwoPtrs.WildThingsTime, sizeof(short)),
                    Bytes(FileTwoPtrs.FlashbackTime, sizeof(short)),
                    Bytes(FileTwoPtrs.EscapeTime, sizeof(short)),
                    Bytes(FileTwoPtrs.DesperateTimesFightTime, sizeof(int)),
                    Bytes(FileTwoPtrs.DesperateTimesFightTime2, sizeof(short)),
                    Bytes(FileTwoPtrs.DesperateTimesGarageTime, sizeof(int)),
                    Bytes(FileTwoPtrs.DesperateTimesGasTime, sizeof(int)),
                    Bytes(FileTwoPtrs.DesperateTimesGasRandom, 1),
                    Bytes(FileTwoPtrs.KilledZombie, 1),
                    Bytes(FileTwoPtrs.InGameFrameCounter, sizeof(int)),
                    Bytes(FileTwoPtrs.ScenarioStatus, 1),
                    Bytes(FileTwoPtrs.Difficulty, 1)
                ),
                OutbreakTrackerMemoryDomains.Scenario
            ),
            Region(
                "PlayerStructObservedSpansFileOne",
                ObservedArray(
                    FileOnePtrs.GetPlayerStartAddress(0),
                    checked((int)(FileOnePtrs.GetPlayerStartAddress(1) - FileOnePtrs.GetPlayerStartAddress(0))),
                    GameConstants.MaxPlayers,
                    ObservedPlayerBytesFileOne
                ),
                OutbreakTrackerMemoryDomains.InGamePlayers
            ),
            Region(
                "PlayerStructObservedSpansFileTwo",
                ObservedArray(
                    FileTwoPtrs.GetPlayerStartAddress(0),
                    checked((int)(FileTwoPtrs.GetPlayerStartAddress(1) - FileTwoPtrs.GetPlayerStartAddress(0))),
                    GameConstants.MaxPlayers,
                    ObservedPlayerBytesFileTwo
                ),
                OutbreakTrackerMemoryDomains.InGamePlayers
            ),
            Region(
                "PlayerDeadInventoryFileOne",
                Bytes(FileOnePtrs.DeadInventoryStart, DeadInventoryBytes),
                OutbreakTrackerMemoryDomains.InGamePlayers
            ),
            Region(
                "PlayerDeadInventoryFileTwo",
                Bytes(FileTwoPtrs.DeadInventoryStart, DeadInventoryBytes),
                OutbreakTrackerMemoryDomains.InGamePlayers
            ),
            Region(
                "PlayerVirusMaxFileOne",
                Bytes(FileOnePtrs.VirusMaxStart, VirusMaxBytes),
                OutbreakTrackerMemoryDomains.InGamePlayers
            ),
            Region(
                "PlayerVirusMaxFileTwo",
                Bytes(FileTwoPtrs.VirusMaxStart, VirusMaxBytes),
                OutbreakTrackerMemoryDomains.InGamePlayers
            ),
            Region(
                "EnemyListFileOne",
                ObservedArray(
                    FileOnePtrs.EnemyListOffset,
                    FileOnePtrs.EnemyListEntrySize,
                    GameConstants.MaxEnemies2,
                    ObservedEnemyListEntryBytes
                ),
                OutbreakTrackerMemoryDomains.Enemies
            ),
            Region(
                "EnemyListFileTwo",
                ObservedArray(
                    FileTwoPtrs.EnemyListOffset,
                    FileTwoPtrs.EnemyListEntrySize,
                    GameConstants.MaxEnemies2,
                    ObservedEnemyListEntryBytes
                ),
                OutbreakTrackerMemoryDomains.Enemies
            ),
            Region(
                "DoorHealthFileOne",
                Union(
                    Bytes(FileOnePtrs.GetDoorHealthAddress(0), sizeof(ushort)),
                    Bytes(FileOnePtrs.GetDoorHealthAddress(FileOneDoorCount - 1), sizeof(ushort))
                ),
                OutbreakTrackerMemoryDomains.Doors
            ),
            Region(
                "DoorHealthFileTwo",
                Union(
                    Bytes(FileTwoPtrs.GetDoorHealthAddress(0), sizeof(ushort)),
                    Bytes(FileTwoPtrs.GetDoorHealthAddress(GameConstants.MaxDoors - 1), sizeof(ushort))
                ),
                OutbreakTrackerMemoryDomains.Doors
            ),
            Region(
                "DoorFlagsFileOne",
                Union(
                    Bytes(FileOnePtrs.GetDoorFlagAddress(0), sizeof(ushort)),
                    Bytes(FileOnePtrs.GetDoorFlagAddress(FileOneDoorCount - 1), sizeof(ushort))
                ),
                OutbreakTrackerMemoryDomains.Doors
            ),
            Region(
                "DoorFlagsFileTwo",
                Union(
                    Bytes(FileTwoPtrs.GetDoorFlagAddress(0), sizeof(ushort)),
                    Bytes(FileTwoPtrs.GetDoorFlagAddress(GameConstants.MaxDoors - 1), sizeof(ushort))
                ),
                OutbreakTrackerMemoryDomains.Doors
            ),
            Region(
                "LobbySlotArrayFileOne",
                ObservedArray(
                    FileOnePtrs.BaseLobbySlot,
                    LobbySlotStructOffsets.StructSize,
                    GameConstants.MaxLobbySlots
                ),
                OutbreakTrackerMemoryDomains.LobbySlots
            ),
            Region(
                "LobbySlotArrayFileTwo",
                ObservedArray(
                    FileTwoPtrs.BaseLobbySlot,
                    LobbySlotStructOffsets.StructSize,
                    GameConstants.MaxLobbySlots
                ),
                OutbreakTrackerMemoryDomains.LobbySlots
            ),
            Region(
                "LobbyRoomPlayersObservedSpansFileOne",
                ObservedArray(
                    FileOnePtrs.GetLobbyRoomPlayerAddress(0),
                    FileOnePtrs.LobbyRoomPlayerStructSize,
                    GameConstants.MaxPlayers,
                    ObservedLobbyRoomPlayerBytesFileOne
                ),
                OutbreakTrackerMemoryDomains.LobbyRoomPlayers
            ),
            Region(
                "LobbyRoomPlayersObservedSpansFileTwo",
                ObservedArray(
                    FileTwoPtrs.GetLobbyRoomPlayerAddress(0),
                    FileTwoPtrs.LobbyRoomPlayerStructSize,
                    GameConstants.MaxPlayers,
                    ObservedLobbyRoomPlayerBytesFileTwo
                ),
                OutbreakTrackerMemoryDomains.LobbyRoomPlayers
            ),
            Region(
                "LobbyRoomMaxPlayersFileOne",
                Bytes(FileOnePtrs.LobbyRoomMaxPlayer, sizeof(short)),
                OutbreakTrackerMemoryDomains.LobbyRoom
            ),
            Region(
                "LobbyRoomMaxPlayersFileTwo",
                Bytes(FileTwoPtrs.LobbyRoomMaxPlayer, sizeof(short)),
                OutbreakTrackerMemoryDomains.LobbyRoom
            ),
            Region(
                "LobbyRoomDifficultyFileOne",
                Bytes(FileOnePtrs.LobbyRoomDifficulty, sizeof(short)),
                OutbreakTrackerMemoryDomains.LobbyRoom
            ),
            Region(
                "LobbyRoomDifficultyFileTwo",
                Bytes(FileTwoPtrs.LobbyRoomDifficulty, sizeof(short)),
                OutbreakTrackerMemoryDomains.LobbyRoom
            ),
            Region(
                "LobbyRoomStatusAndScenarioFileOne",
                Union(Bytes(FileOnePtrs.LobbyRoomStatus, 1), Bytes(FileOnePtrs.LobbyRoomScenarioId, sizeof(short))),
                OutbreakTrackerMemoryDomains.LobbyRoom
            ),
            Region(
                "LobbyRoomStatusAndScenarioFileTwo",
                Union(Bytes(FileTwoPtrs.LobbyRoomStatus, 1), Bytes(FileTwoPtrs.LobbyRoomScenarioId, sizeof(short))),
                OutbreakTrackerMemoryDomains.LobbyRoom
            ),
            Region(
                "LobbyRoomTimeFileOne",
                Bytes(FileOnePtrs.LobbyRoomTime, sizeof(short)),
                OutbreakTrackerMemoryDomains.LobbyRoom
            ),
            Region(
                "LobbyRoomTimeFileTwo",
                Bytes(FileTwoPtrs.LobbyRoomTime, sizeof(short)),
                OutbreakTrackerMemoryDomains.LobbyRoom
            ),
            Region(
                "LobbyRoomCurrentPlayerFileOne",
                Bytes(FileOnePtrs.LobbyRoomCurPlayer, sizeof(short)),
                OutbreakTrackerMemoryDomains.LobbyRoom
            ),
            Region(
                "LobbyRoomCurrentPlayerFileTwo",
                Bytes(FileTwoPtrs.LobbyRoomCurPlayer, sizeof(short)),
                OutbreakTrackerMemoryDomains.LobbyRoom
            ),
            Region(
                "LobbyRoomPriorityFileTwo",
                Bytes(FileTwoPtrs.RoomPriority, FileTwoPtrs.RoomPriorityEntrySize * FileTwoPtrs.RoomPriorityCount),
                OutbreakTrackerMemoryDomains.LobbyRoom
            ),
        ];
    }

    private static RelativeMemoryRange Bytes(nint start, int byteLength) => Bytes(start, checked((nuint)byteLength));

    private static RelativeMemoryRange Bytes(nint start, nuint byteLength) => new(start, byteLength);

    private static RelativeMemoryRange ObservedArray(nint start, int strideBytes, int elementCount) =>
        ObservedArray(start, strideBytes, elementCount, strideBytes);

    private static RelativeMemoryRange ObservedArray(
        nint start,
        int strideBytes,
        int elementCount,
        int observedBytesPerElement
    )
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(elementCount, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(strideBytes, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(observedBytesPerElement, 0);

        long byteLength = ((long)(elementCount - 1) * strideBytes) + observedBytesPerElement;
        return new(start, checked((nuint)byteLength));
    }

    private static RelativeMemoryRange Union(params ReadOnlySpan<RelativeMemoryRange> ranges)
    {
        if (ranges.IsEmpty)
        {
            throw new ArgumentException("At least one range is required.", nameof(ranges));
        }

        nint start = ranges[0].Start;
        nint endExclusive = ranges[0].EndExclusive;
        for (int i = 1; i < ranges.Length; i++)
        {
            RelativeMemoryRange range = ranges[i];
            if (range.Start < start)
            {
                start = range.Start;
            }

            if (range.EndExclusive > endExclusive)
            {
                endExclusive = range.EndExclusive;
            }
        }

        return new(start, checked((nuint)(endExclusive - start)));
    }

    private readonly record struct RelativeMemoryRange(nint Start, nuint ByteLength)
    {
        public nint EndExclusive => checked(Start + (nint)ByteLength);
    }

    private readonly record struct RelativeRegionDefinition(
        string Name,
        nint StartOffset,
        nuint ByteLength,
        OutbreakTrackerMemoryDomains Domains
    );
}

public sealed class MemoryWatcherSnapshotCache(
    IMemoryWatchSessionFactory sessionFactory,
    IOutbreakTrackerMemoryRegionCatalog regionCatalog,
    MemoryWatcherSettings settings,
    ILogger<MemoryWatcherSnapshotCache> logger
) : IMemoryActivitySource, IDisposable
{
    // OT2 still exposes grouped snapshots to the rest of the app, but wait-capable MemoryWatcher
    // backends can now use this seam to skip redundant snapshot reads between real region signals.
    private static readonly long RefreshIntervalTicks = Stopwatch.Frequency / 500;
    private static readonly TimeSpan EEmemReadyTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan EEmemReadyFallbackPollInterval = TimeSpan.FromMilliseconds(25);
    private static readonly TimeSpan EEmemReadyWaitSlice = TimeSpan.FromMilliseconds(250);
    private const int EEmemPointerSizeBytes = sizeof(long);
    private readonly IMemoryWatchSessionFactory _sessionFactory = sessionFactory;
    private readonly IOutbreakTrackerMemoryRegionCatalog _regionCatalog = regionCatalog;
    private readonly MemoryWatcherSettings _settings = settings;
    private readonly ILogger<MemoryWatcherSnapshotCache> _logger = logger;
    private readonly Lock _sync = new();
    private IMemoryWatchSession? _readSession;
    private IMemoryWatchSession? _activitySession;
    private MemoryWatchActivitySource? _activitySource;
    private ActivityWatch[] _activityWatches = [];
    private ActivityWatchDescriptor[] _activityDescriptors = [];
    private WatchedRegion[] _regions = [];

    public async ValueTask<nint> AttachAsync(IGameClient gameClient, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(gameClient);
        cancellationToken.ThrowIfCancellationRequested();

        string moduleName =
            gameClient.Process.GetSafeName()
            ?? throw new InvalidOperationException("The attached game client process is unavailable.");
        IReadOnlyList<int> hardwareThreadIds = gameClient.Process.GetSafeThreadIds();
        IMemoryWatchSession readSession = _sessionFactory.Open(
            gameClient.Process!.Id,
            CreateReadSessionOptions(hardwareThreadIds)
        );

        try
        {
            nint eememBaseAddress = await ResolveEEmemBaseAddressAsync(
                    readSession,
                    gameClient,
                    moduleName,
                    cancellationToken
                )
                .ConfigureAwait(false);
            IReadOnlyList<OutbreakTrackerMemoryRegionDefinition> regions = _regionCatalog.CreateRegions(
                eememBaseAddress
            );
            MemoryWatchGameFileDetector.TryDetectActiveGameFile(
                readSession,
                eememBaseAddress,
                out GameFile activeGameFile
            );
            OutbreakTrackerMemoryWatchPlanItem[] readPlan = MemoryWatchActivityPlanFactory.CreateReadPlan(regions);
            WatchedRegion[] watched = new WatchedRegion[regions.Count];
            try
            {
                for (int i = 0; i < readPlan.Length; i++)
                {
                    OutbreakTrackerMemoryWatchPlanItem planItem = readPlan[i];
                    IMemoryWatchHandle handle = readSession.CreateWatch(planItem.Region);
                    watched[i] = new WatchedRegion(
                        planItem,
                        handle,
                        new byte[checked((int)planItem.Region.ByteLength)]
                    );
                }
            }
            catch
            {
                foreach (WatchedRegion region in watched)
                {
                    region?.Dispose();
                }

                throw;
            }

            ActivityPipeline activityPipeline = BuildActivityPipeline(
                gameClient.Process.Id,
                hardwareThreadIds,
                eememBaseAddress,
                activeGameFile,
                regions,
                watched
            );

            lock (_sync)
            {
                DisposeCore();
                _readSession = readSession;
                _activitySession = activityPipeline.Session;
                _activitySource = activityPipeline.ActivitySource;
                _activityWatches = activityPipeline.DedicatedWatches;
                _activityDescriptors = activityPipeline.Descriptors;
                _regions = watched;
            }

            _logger.LogInformation(
                "MemoryWatcher snapshot cache attached to PID {ProcessId} with EEmem base 0x{BaseAddress:X}. Regions: {RegionCount}",
                gameClient.Process.Id,
                eememBaseAddress,
                watched.Length
            );

            await ValueTask.CompletedTask.ConfigureAwait(false);
            return eememBaseAddress;
        }
        catch
        {
            readSession.Dispose();
            throw;
        }
    }

    public ValueTask<OutbreakTrackerMemoryActivityResult> WaitForActivityAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken
    )
    {
        if (timeout <= TimeSpan.Zero)
        {
            return ValueTask.FromResult(OutbreakTrackerMemoryActivityResult.TimedOut(Stopwatch.GetTimestamp()));
        }

        MemoryWatchActivitySource? activitySource;
        ActivityWatchDescriptor[] descriptors;
        lock (_sync)
        {
            activitySource = _activitySource;
            descriptors = _activityDescriptors;
        }

        return activitySource is null
            ? WaitByTimeoutAsync(timeout, cancellationToken)
            : WaitForUpstreamActivityAsync(activitySource, descriptors, timeout, cancellationToken);
    }

    private async ValueTask<nint> ResolveEEmemBaseAddressAsync(
        IMemoryWatchSession session,
        IGameClient gameClient,
        string moduleName,
        CancellationToken cancellationToken
    )
    {
        Stopwatch readyTimer = Stopwatch.StartNew();
        using IMemoryWatchHandle exportHandle = await CreateEEmemExportWatchAsync(
                session,
                gameClient,
                moduleName,
                readyTimer,
                cancellationToken
            )
            .ConfigureAwait(false);

        _logger.LogDebug(
            "Waiting for exported pointer {ModuleName}!EEmem to become non-zero before OT2 attaches grouped region watches.",
            moduleName
        );

        bool waitSupported = true;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (TryReadEEmemPointer(exportHandle, out nint eememBaseAddress) && eememBaseAddress != nint.Zero)
            {
                return eememBaseAddress;
            }

            TimeSpan remaining = GetRemainingReadyTime(readyTimer);
            if (remaining <= TimeSpan.Zero)
            {
                throw new InvalidOperationException(
                    $"Timed out waiting for exported pointer '{moduleName}!EEmem' to become ready."
                );
            }

            if (waitSupported)
            {
                WatchSignalResult wait = exportHandle.WaitForSignal(
                    remaining < EEmemReadyWaitSlice ? remaining : EEmemReadyWaitSlice
                );
                switch (wait.Status)
                {
                    case WatchSignalStatus.Signaled:
                    case WatchSignalStatus.TimedOut:
                        continue;
                    case WatchSignalStatus.Unsupported:
                    case WatchSignalStatus.BackendUnavailable:
                        waitSupported = false;
                        _logger.LogDebug(
                            "MemoryWatcher backend does not support signal waits for {ModuleName}!EEmem; using low-frequency readiness polling instead.",
                            moduleName
                        );
                        break;
                    case WatchSignalStatus.Disposed:
                        throw new InvalidOperationException(
                            $"MemoryWatcher disposed the readiness watch for '{moduleName}!EEmem' before the pointer became ready."
                        );
                }
            }

            await Task.Delay(GetFallbackDelay(GetRemainingReadyTime(readyTimer)), cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async ValueTask<IMemoryWatchHandle> CreateEEmemExportWatchAsync(
        IMemoryWatchSession session,
        IGameClient gameClient,
        string moduleName,
        Stopwatch readyTimer,
        CancellationToken cancellationToken
    )
    {
        Exception? lastError = null;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (
                    !MemoryWatcherEEmemSpecFactory.TryCreateEEmemExportPointerSpec(
                        gameClient,
                        moduleName,
                        out MemoryRegionSpec exportPointerSpec
                    )
                )
                {
                    TimeSpan remaining = GetRemainingReadyTime(readyTimer);
                    if (remaining <= TimeSpan.Zero)
                    {
                        throw new InvalidOperationException(
                            $"Failed to resolve exported pointer '{moduleName}!EEmem' before timeout."
                        );
                    }

                    await Task.Delay(GetFallbackDelay(remaining), cancellationToken).ConfigureAwait(false);
                    continue;
                }

                return session.CreateWatch(exportPointerSpec);
            }
            catch (InvalidOperationException ex)
            {
                lastError = ex;
                TimeSpan remaining = GetRemainingReadyTime(readyTimer);
                if (remaining <= TimeSpan.Zero)
                {
                    throw new InvalidOperationException(
                        $"Failed to resolve exported pointer '{moduleName}!EEmem' before timeout.",
                        lastError
                    );
                }

                await Task.Delay(GetFallbackDelay(remaining), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static bool TryReadEEmemPointer(IMemoryWatchHandle exportHandle, out nint eememBaseAddress)
    {
        Span<byte> snapshot = stackalloc byte[EEmemPointerSizeBytes];
        if (!exportHandle.TryReadSnapshot(snapshot, out int bytesRead) || bytesRead < EEmemPointerSizeBytes)
        {
            eememBaseAddress = nint.Zero;
            return false;
        }

        eememBaseAddress = checked((nint)BinaryPrimitives.ReadUInt64LittleEndian(snapshot));
        return true;
    }

    private static TimeSpan GetRemainingReadyTime(Stopwatch readyTimer)
    {
        TimeSpan remaining = EEmemReadyTimeout - readyTimer.Elapsed;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    private static TimeSpan GetFallbackDelay(TimeSpan remaining)
    {
        TimeSpan capped = remaining < EEmemReadyFallbackPollInterval ? remaining : EEmemReadyFallbackPollInterval;
        return capped > TimeSpan.Zero ? capped : TimeSpan.FromMilliseconds(1);
    }

    private static async ValueTask<OutbreakTrackerMemoryActivityResult> WaitByTimeoutAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken
    )
    {
        await Task.Delay(timeout, cancellationToken).ConfigureAwait(false);
        return OutbreakTrackerMemoryActivityResult.TimedOut(Stopwatch.GetTimestamp());
    }

    private async ValueTask<OutbreakTrackerMemoryActivityResult> WaitForUpstreamActivityAsync(
        MemoryWatchActivitySource activitySource,
        ActivityWatchDescriptor[] activityDescriptors,
        TimeSpan timeout,
        CancellationToken cancellationToken
    )
    {
        WatchSignalResult wait = await activitySource
            .WaitForActivityAsync(timeout, cancellationToken)
            .ConfigureAwait(false);
        if (wait.Status != WatchSignalStatus.Signaled)
        {
            return wait.Status switch
            {
                WatchSignalStatus.TimedOut => OutbreakTrackerMemoryActivityResult.TimedOut(wait.Timestamp),
                WatchSignalStatus.Unsupported => OutbreakTrackerMemoryActivityResult.Unsupported(wait.Timestamp),
                WatchSignalStatus.Disposed => OutbreakTrackerMemoryActivityResult.Disposed(wait.Timestamp),
                WatchSignalStatus.BackendUnavailable => OutbreakTrackerMemoryActivityResult.BackendUnavailable(
                    wait.Timestamp
                ),
                _ => OutbreakTrackerMemoryActivityResult.TimedOut(wait.Timestamp),
            };
        }

        MemoryWatchActivity[] activities = new MemoryWatchActivity[activityDescriptors.Length];
        MemoryWatchActivityPollResult poll = activitySource.PollActivities(activities);
        if (poll.Status != WatchSignalStatus.Signaled || poll.Written <= 0)
        {
            return poll.Status switch
            {
                WatchSignalStatus.TimedOut => OutbreakTrackerMemoryActivityResult.TimedOut(poll.Timestamp),
                WatchSignalStatus.Unsupported => OutbreakTrackerMemoryActivityResult.Unsupported(poll.Timestamp),
                WatchSignalStatus.Disposed => OutbreakTrackerMemoryActivityResult.Disposed(poll.Timestamp),
                WatchSignalStatus.BackendUnavailable => OutbreakTrackerMemoryActivityResult.BackendUnavailable(
                    poll.Timestamp
                ),
                _ => OutbreakTrackerMemoryActivityResult.TimedOut(poll.Timestamp),
            };
        }

        OutbreakTrackerMemoryDomains domains = OutbreakTrackerMemoryDomains.None;
        bool requiresSnapshotRefresh = false;
        for (int i = 0; i < poll.Written; i++)
        {
            int watchIndex = activities[i].WatchIndex;
            if ((uint)watchIndex >= (uint)activityDescriptors.Length)
            {
                continue;
            }

            ActivityWatchDescriptor descriptor = activityDescriptors[watchIndex];
            domains |= descriptor.Domains;
            requiresSnapshotRefresh |= descriptor.RequiresSnapshotRefresh;
        }

        if (requiresSnapshotRefresh && domains != OutbreakTrackerMemoryDomains.None)
        {
            MarkRegionsForForcedRefresh(domains);
        }

        return domains == OutbreakTrackerMemoryDomains.None
            ? OutbreakTrackerMemoryActivityResult.TimedOut(poll.Timestamp)
            : OutbreakTrackerMemoryActivityResult.Signaled(domains, poll.Timestamp);
    }

    private void MarkRegionsForForcedRefresh(OutbreakTrackerMemoryDomains domains)
    {
        lock (_sync)
        {
            foreach (WatchedRegion region in _regions)
            {
                if ((region.Domains & domains) != OutbreakTrackerMemoryDomains.None)
                {
                    region.RequestForcedRefresh();
                }
            }
        }
    }

    public void Detach()
    {
        lock (_sync)
        {
            DisposeCore();
        }
    }

    public bool TryRead(nint address, Span<byte> destination, out int bytesRead)
    {
        bytesRead = 0;
        if (destination.IsEmpty || address == nint.Zero)
        {
            return false;
        }

        lock (_sync)
        {
            foreach (WatchedRegion region in _regions)
            {
                if (!region.Contains(address, destination.Length))
                {
                    continue;
                }

                if (!region.TryRefresh())
                {
                    return false;
                }

                bytesRead = region.CopyTo(address, destination);
                return bytesRead == destination.Length;
            }
        }

        return false;
    }

    public void Dispose()
    {
        lock (_sync)
        {
            DisposeCore();
        }
    }

    private void DisposeCore()
    {
        _activitySource?.Dispose();
        _activitySource = null;

        foreach (ActivityWatch watch in _activityWatches)
        {
            watch.Dispose();
        }

        _activityWatches = [];
        _activityDescriptors = [];

        foreach (WatchedRegion region in _regions)
        {
            region.Dispose();
        }

        _regions = [];
        _activitySession?.Dispose();
        _activitySession = null;
        _readSession?.Dispose();
        _readSession = null;
    }

    private ActivityPipeline BuildActivityPipeline(
        int processId,
        IReadOnlyList<int> hardwareThreadIds,
        nint eememBaseAddress,
        GameFile activeGameFile,
        IReadOnlyList<OutbreakTrackerMemoryRegionDefinition> regions,
        WatchedRegion[] watchedRegions
    )
    {
        if (
            !MemoryWatchActivityPlanFactory.SupportsDedicatedActivity(_settings.PreferredBackend)
            || !MemoryWatchActivityPlanFactory.TryCreateActivityPlan(
                _settings.PreferredBackend,
                eememBaseAddress,
                activeGameFile,
                regions,
                out OutbreakTrackerMemoryWatchPlanItem[] plan
            )
        )
        {
            return CreateReadDrivenActivityPipeline(watchedRegions);
        }

        try
        {
            IMemoryWatchSession activitySession = _sessionFactory.Open(
                processId,
                _settings.ToSessionOptions(hardwareThreadIds)
            );
            try
            {
                ActivityWatch[] activityWatches = new ActivityWatch[plan.Length];
                for (int i = 0; i < plan.Length; i++)
                {
                    IMemoryWatchHandle handle = activitySession.CreateWatch(plan[i].Region);
                    activityWatches[i] = new ActivityWatch(plan[i].Name, handle, plan[i].Domains);
                }

                _logger.LogInformation(
                    "MemoryWatcher activity session opened with backend request {Backend}; OT2 will use {WatchCount} dedicated wake watches alongside grouped snapshot activity.",
                    _settings.PreferredBackend,
                    activityWatches.Length
                );

                ActivityPipeline combinedPipeline = CreateCombinedActivityPipeline(
                    watchedRegions,
                    activitySession,
                    activityWatches
                );
                return new ActivityPipeline(
                    combinedPipeline.Session,
                    combinedPipeline.ActivitySource,
                    combinedPipeline.DedicatedWatches,
                    combinedPipeline.Descriptors
                );
            }
            catch
            {
                activitySession.Dispose();
                throw;
            }
        }
        catch (Exception ex)
            when (ex is InvalidOperationException or IOException or UnauthorizedAccessException or NotSupportedException
            )
        {
            if (!_settings.AllowFallback)
            {
                _logger.LogWarning(
                    ex,
                    "MemoryWatcher could not open the requested dedicated activity backend {Backend}; OT2 will fall back to timeout-driven refreshes because fallback is disabled.",
                    _settings.PreferredBackend
                );
                return new ActivityPipeline(null, null, [], []);
            }

            _logger.LogInformation(
                ex,
                "MemoryWatcher could not open the requested dedicated activity backend {Backend}; OT2 will fall back to read-driven snapshot waits.",
                _settings.PreferredBackend
            );
            return CreateReadDrivenActivityPipeline(watchedRegions);
        }
    }

    private static ActivityPipeline CreateReadDrivenActivityPipeline(WatchedRegion[] watchedRegions) =>
        new(
            null,
            new MemoryWatchActivitySource(watchedRegions.Select(static region => region.Handle).ToArray()),
            [],
            watchedRegions
                .Select(static region => new ActivityWatchDescriptor(region.Domains, RequiresSnapshotRefresh: false))
                .ToArray()
        );

    private static ActivityPipeline CreateCombinedActivityPipeline(
        WatchedRegion[] watchedRegions,
        IMemoryWatchSession activitySession,
        ActivityWatch[] activityWatches
    )
    {
        IMemoryWatchHandle[] handles = new IMemoryWatchHandle[watchedRegions.Length + activityWatches.Length];
        ActivityWatchDescriptor[] descriptors = new ActivityWatchDescriptor[handles.Length];

        int writeIndex = 0;
        for (int i = 0; i < watchedRegions.Length; i++, writeIndex++)
        {
            handles[writeIndex] = watchedRegions[i].Handle;
            descriptors[writeIndex] = new ActivityWatchDescriptor(
                watchedRegions[i].Domains,
                RequiresSnapshotRefresh: false
            );
        }

        for (int i = 0; i < activityWatches.Length; i++, writeIndex++)
        {
            handles[writeIndex] = activityWatches[i].Handle;
            descriptors[writeIndex] = new ActivityWatchDescriptor(
                activityWatches[i].Domains,
                RequiresSnapshotRefresh: true
            );
        }

        return new ActivityPipeline(
            activitySession,
            new MemoryWatchActivitySource(handles),
            activityWatches,
            descriptors
        );
    }

    private MemoryWatchSessionOptions CreateReadSessionOptions(IReadOnlyList<int> hardwareThreadIds)
    {
        WatchBackendKind preferredBackend = _settings.PreferredBackend switch
        {
            WatchBackendKind.Snapshot or WatchBackendKind.HashIndexedSnapshot or WatchBackendKind.SegmentedSnapshot =>
                _settings.PreferredBackend,
            _ => WatchBackendKind.Auto,
        };

        return new MemoryWatchSessionOptions
        {
            PreferredBackend = preferredBackend,
            PreferredPrecision = WatchPrecision.SnapshotBitExact,
            AllowFallback = true,
            AllowIntrusiveBackends = false,
            AllowNativeAgent = false,
            HardwareThreadIds = hardwareThreadIds ?? Array.Empty<int>(),
            EventBufferCapacity = _settings.EventBufferCapacity,
            HashBlockSizeBytes = _settings.HashBlockSizeBytes,
            UseHashIndex = _settings.UseHashIndex,
        };
    }

    private readonly record struct ActivityPipeline(
        IMemoryWatchSession? Session,
        MemoryWatchActivitySource? ActivitySource,
        ActivityWatch[] DedicatedWatches,
        ActivityWatchDescriptor[] Descriptors
    );

    private readonly record struct ActivityWatchDescriptor(
        OutbreakTrackerMemoryDomains Domains,
        bool RequiresSnapshotRefresh
    );

    private sealed class ActivityWatch(string name, IMemoryWatchHandle handle, OutbreakTrackerMemoryDomains domains)
        : IDisposable
    {
        public string Name { get; } = name;

        public IMemoryWatchHandle Handle { get; } = handle;

        public OutbreakTrackerMemoryDomains Domains { get; } = domains;

        public void Dispose() => Handle.Dispose();
    }

    private sealed class WatchedRegion : IDisposable
    {
        private readonly OutbreakTrackerMemoryWatchPlanItem _planItem;
        private readonly IMemoryWatchHandle _handle;
        private readonly byte[] _buffer;
        private bool _forceReadOnNextRefresh;
        private bool _hasConsumedSignalSequence;
        private bool _isInitialized;
        private bool _waitSupported = true;
        private long _lastRefreshTicks;
        private ulong _lastConsumedSignalSequence;

        public WatchedRegion(OutbreakTrackerMemoryWatchPlanItem planItem, IMemoryWatchHandle handle, byte[] buffer)
        {
            _planItem = planItem;
            _handle = handle;
            _buffer = buffer;
        }

        public bool Contains(nint address, int byteCount)
        {
            nint endExclusive = address + byteCount;
            return address >= _planItem.Region.Address
                && endExclusive <= _planItem.Region.Address + (nint)_planItem.Region.ByteLength;
        }

        public string Name => _planItem.Name;

        public OutbreakTrackerMemoryDomains Domains => _planItem.Domains;

        public IMemoryWatchHandle Handle => _handle;

        public WatchSignalResult WaitForSignal(TimeSpan timeout) => _handle.WaitForSignal(timeout);

        public void RequestForcedRefresh() => _forceReadOnNextRefresh = true;

        public bool TryRefresh()
        {
            long now = Stopwatch.GetTimestamp();
            if (!_isInitialized)
            {
                return TryReadSnapshot(now, captureWaitBaseline: true);
            }

            if (_forceReadOnNextRefresh)
            {
                bool forceReadSucceeded = TryReadSnapshot(now, captureWaitBaseline: false);
                if (forceReadSucceeded)
                {
                    _forceReadOnNextRefresh = false;
                }

                return forceReadSucceeded;
            }

            if (_waitSupported && TryRefreshFromWait(now, out bool refreshed))
            {
                return refreshed;
            }

            if (_lastRefreshTicks != 0 && now - _lastRefreshTicks <= RefreshIntervalTicks)
            {
                return true;
            }

            return TryReadSnapshot(now, captureWaitBaseline: false);
        }

        private bool TryRefreshFromWait(long now, out bool refreshed)
        {
            refreshed = false;

            WatchSignalResult wait = _handle.WaitForSignal(TimeSpan.Zero);
            switch (wait.Status)
            {
                case WatchSignalStatus.Signaled:
                    if (_hasConsumedSignalSequence && wait.SignalSequence == _lastConsumedSignalSequence)
                    {
                        refreshed = true;
                        return true;
                    }

                    refreshed = TryReadSnapshot(now, captureWaitBaseline: false);
                    if (refreshed)
                    {
                        _hasConsumedSignalSequence = true;
                        _lastConsumedSignalSequence = wait.SignalSequence;
                    }

                    return true;
                case WatchSignalStatus.TimedOut:
                    refreshed = true;
                    return true;
                case WatchSignalStatus.Unsupported:
                case WatchSignalStatus.BackendUnavailable:
                    _waitSupported = false;
                    return false;
                case WatchSignalStatus.Disposed:
                default:
                    refreshed = false;
                    return true;
            }
        }

        private bool TryReadSnapshot(long now, bool captureWaitBaseline)
        {
            if (!_handle.TryReadSnapshot(_buffer, out int bytesRead) || bytesRead < _buffer.Length)
            {
                return false;
            }

            _isInitialized = true;
            _lastRefreshTicks = now;

            if (captureWaitBaseline && _waitSupported)
            {
                CaptureWaitBaseline();
            }

            return true;
        }

        public int CopyTo(nint address, Span<byte> destination)
        {
            int offset = checked((int)(address - _planItem.Region.Address));
            _buffer.AsSpan(offset, destination.Length).CopyTo(destination);
            return destination.Length;
        }

        public void Dispose() => _handle.Dispose();

        private void CaptureWaitBaseline()
        {
            WatchSignalResult wait = _handle.WaitForSignal(TimeSpan.Zero);
            switch (wait.Status)
            {
                case WatchSignalStatus.Signaled:
                    _hasConsumedSignalSequence = true;
                    _lastConsumedSignalSequence = wait.SignalSequence;
                    break;
                case WatchSignalStatus.TimedOut:
                    break;
                case WatchSignalStatus.Unsupported:
                case WatchSignalStatus.BackendUnavailable:
                    _waitSupported = false;
                    break;
                case WatchSignalStatus.Disposed:
                default:
                    break;
            }
        }
    }
}

public sealed class SnapshotBackedSafeMemoryReader(
    MemoryWatcherSnapshotCache snapshotCache,
    ILogger<SnapshotBackedSafeMemoryReader> logger
) : ISafeMemoryReader
{
    private readonly MemoryWatcherSnapshotCache _snapshotCache = snapshotCache;
    private readonly ILogger<SnapshotBackedSafeMemoryReader> _logger = logger;

    public T Read<T>(nint hProcess, nint address)
        where T : unmanaged
    {
        int size = Marshal.SizeOf<T>();
        if (size <= 0)
        {
            return default;
        }

        byte[] buffer = ArrayPool<byte>.Shared.Rent(size);
        try
        {
            ReadExactly(hProcess, address, buffer.AsSpan(0, size));
            return MemoryMarshal.Read<T>(buffer.AsSpan(0, size));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public T ReadStruct<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors
        )]
            T
    >(nint hProcess, nint address)
        where T : struct
    {
        int size = Marshal.SizeOf<T>();
        if (size <= 0)
        {
            return default;
        }

        byte[] buffer = ArrayPool<byte>.Shared.Rent(size);
        try
        {
            ReadExactly(hProcess, address, buffer.AsSpan(0, size));
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            }
            finally
            {
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private void ReadExactly(nint hProcess, nint address, Span<byte> destination)
    {
        if (_snapshotCache.TryRead(address, destination, out int bytesRead) && bytesRead == destination.Length)
        {
            return;
        }

        if (OperatingSystem.IsWindows())
        {
            byte[] copy = destination.ToArray();
            if (
                !SafeNativeMethods.ReadProcessMemory(hProcess, address, copy, copy.Length, out int read)
                || read != copy.Length
            )
            {
                int error = Marshal.GetLastPInvokeError();
                throw new Win32Exception(error, $"Failed to read process memory at address 0x{address:X}.");
            }

            copy.AsSpan().CopyTo(destination);
            return;
        }

        if (OperatingSystem.IsLinux())
        {
            ReadLinux(hProcess, address, destination);
            return;
        }

        throw new PlatformNotSupportedException("SnapshotBackedSafeMemoryReader only supports Windows and Linux.");
    }

    private static unsafe void ReadLinux(nint hProcess, nint address, Span<byte> destination)
    {
        fixed (byte* destinationPointer = destination)
        {
            Iovec local = new() { iov_base = (nint)destinationPointer, iov_len = (nuint)destination.Length };
            Iovec remote = new() { iov_base = address, iov_len = (nuint)destination.Length };

            long bytesRead = LinuxNativeMethods.ProcessVmReadv((int)hProcess, ref local, 1, ref remote, 1, 0);
            if (bytesRead != destination.Length)
            {
                int errno = Marshal.GetLastPInvokeError();
                throw new Win32Exception(errno, $"process_vm_readv failed at address 0x{address:X}.");
            }
        }
    }
}

public sealed class SnapshotBackedStringReader(
    MemoryWatcherSnapshotCache snapshotCache,
    ILogger<SnapshotBackedStringReader> logger
) : IStringReader
{
    private readonly MemoryWatcherSnapshotCache _snapshotCache = snapshotCache;
    private readonly ILogger<SnapshotBackedStringReader> _logger = logger;

    static SnapshotBackedStringReader()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public bool TryRead(nint hProcess, nint address, out string result, Encoding? encoding = null)
    {
        const int maxSafeLength = 1048576;
        const int chunkSize = 256;

        if (address == nint.Zero)
        {
            result = string.Empty;
            return true;
        }

        encoding ??= Encoding.GetEncoding(932);
        List<byte> bytes = new(chunkSize);
        byte[] chunk = new byte[chunkSize];

        while (bytes.Count < maxSafeLength)
        {
            int toRead = Math.Min(chunkSize, maxSafeLength - bytes.Count);
            Span<byte> slice = chunk.AsSpan(0, toRead);
            if (!_snapshotCache.TryRead(address + bytes.Count, slice, out int bytesRead) || bytesRead <= 0)
            {
                if (!TryReadDirect(hProcess, address + bytes.Count, slice, out bytesRead) || bytesRead <= 0)
                {
                    break;
                }
            }

            bool foundTerminator = false;
            for (int i = 0; i < bytesRead; i++)
            {
                if (slice[i] == 0)
                {
                    foundTerminator = true;
                    break;
                }

                bytes.Add(slice[i]);
            }

            if (foundTerminator)
            {
                break;
            }
        }

        result = bytes.Count == 0 ? string.Empty : encoding.GetString([.. bytes]);
        return true;
    }

    public string Read(nint hProcess, nint address, Encoding? encoding = null) =>
        TryRead(hProcess, address, out string result, encoding) ? result : string.Empty;

    private bool TryReadDirect(nint hProcess, nint address, Span<byte> destination, out int bytesRead)
    {
        if (OperatingSystem.IsWindows())
        {
            byte[] buffer = destination.ToArray();
            bool success = SafeNativeMethods.ReadProcessMemory(hProcess, address, buffer, buffer.Length, out bytesRead);
            if (success && bytesRead > 0)
            {
                buffer.AsSpan(0, bytesRead).CopyTo(destination);
            }

            return success;
        }

        if (OperatingSystem.IsLinux())
        {
            unsafe
            {
                fixed (byte* destinationPointer = destination)
                {
                    Iovec local = new() { iov_base = (nint)destinationPointer, iov_len = (nuint)destination.Length };
                    Iovec remote = new() { iov_base = address, iov_len = (nuint)destination.Length };

                    long read = LinuxNativeMethods.ProcessVmReadv((int)hProcess, ref local, 1, ref remote, 1, 0);
                    bytesRead = read > 0 ? checked((int)read) : 0;
                    return read > 0;
                }
            }
        }

        bytesRead = 0;
        return false;
    }
}

public sealed class MemoryWatcherEEmemMemory(
    ISafeMemoryReader memoryReader,
    IStringReader stringReader,
    MemoryWatcherSnapshotCache snapshotCache,
    ILogger<MemoryWatcherEEmemMemory> logger
) : IEEmemMemory
{
    private readonly MemoryWatcherSnapshotCache _snapshotCache = snapshotCache;
    private readonly ILogger<MemoryWatcherEEmemMemory> _logger = logger;
    private IGameClient? _gameClient;

    public nint BaseAddress { get; private set; }

    public ISafeMemoryReader MemoryReader { get; } = memoryReader;

    public IStringReader StringReader { get; } = stringReader;

    public async ValueTask<bool> InitializeAsync(IGameClient gameClient, CancellationToken cancellationToken)
    {
        _gameClient = gameClient ?? throw new ArgumentNullException(nameof(gameClient));

        try
        {
            BaseAddress = await _snapshotCache.AttachAsync(gameClient, cancellationToken).ConfigureAwait(false);
            bool success = BaseAddress != nint.Zero;
            if (success)
            {
                _logger.LogInformation("Resolved EEmem base through MemoryWatcher at 0x{BaseAddress:X}", BaseAddress);
            }

            return success;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _snapshotCache.Detach();
            BaseAddress = nint.Zero;
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize EEmem through MemoryWatcher.");
            _snapshotCache.Detach();
            BaseAddress = nint.Zero;
            return false;
        }
    }

    public nint GetAddressFromPtr(nint ptrOffset) => BaseAddress + ptrOffset;

    public nint GetAddressFromPtrChain(nint ptrOffset, params ReadOnlySpan<nint> offsets)
    {
        if (_gameClient is null)
        {
            throw new InvalidOperationException("EEmemMemory has not been initialized.");
        }

        // OT2 offset "chains" are EE-relative struct offsets, not host-process pointer dereferences.
        return EEmemOffsetResolver.Resolve(BaseAddress, ptrOffset, offsets);
    }

    public bool IsAddressInBounds(nint address)
    {
        if (BaseAddress == nint.Zero)
        {
            return false;
        }

        const nint eememSize = 32 * 1024 * 1024;
        return address >= BaseAddress && address < BaseAddress + eememSize;
    }
}
