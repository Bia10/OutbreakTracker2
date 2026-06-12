using MemoryWatcher;

namespace OutbreakTracker2.MemoryWatcherIntegration;

[Flags]
public enum OutbreakTrackerMemoryDomains
{
    None = 0,
    Scenario = 1 << 0,
    InGamePlayers = 1 << 1,
    Enemies = 1 << 2,
    Doors = 1 << 3,
    LobbyRoom = 1 << 4,
    LobbyRoomPlayers = 1 << 5,
    LobbySlots = 1 << 6,
}

public readonly record struct OutbreakTrackerMemoryActivityResult(
    WatchSignalStatus Status,
    OutbreakTrackerMemoryDomains Domains,
    long Timestamp
)
{
    public bool HasActivity => Status == WatchSignalStatus.Signaled && Domains != OutbreakTrackerMemoryDomains.None;

    public static OutbreakTrackerMemoryActivityResult Signaled(OutbreakTrackerMemoryDomains domains, long timestamp) =>
        new(WatchSignalStatus.Signaled, domains, timestamp);

    public static OutbreakTrackerMemoryActivityResult TimedOut(long timestamp) =>
        new(WatchSignalStatus.TimedOut, OutbreakTrackerMemoryDomains.None, timestamp);

    public static OutbreakTrackerMemoryActivityResult Unsupported(long timestamp) =>
        new(WatchSignalStatus.Unsupported, OutbreakTrackerMemoryDomains.None, timestamp);

    public static OutbreakTrackerMemoryActivityResult Disposed(long timestamp) =>
        new(WatchSignalStatus.Disposed, OutbreakTrackerMemoryDomains.None, timestamp);

    public static OutbreakTrackerMemoryActivityResult BackendUnavailable(long timestamp) =>
        new(WatchSignalStatus.BackendUnavailable, OutbreakTrackerMemoryDomains.None, timestamp);
}
