using MemoryWatcher.Remote;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;

namespace OutbreakTracker2.MemoryWatcherIntegration;

internal static class MemoryWatchGameFileDetector
{
    private const byte DiscSignatureByte = 0x53;

    public static bool TryDetectActiveGameFile(
        IMemoryWatchSession session,
        nint eememBaseAddress,
        out GameFile activeGameFile
    )
    {
        ArgumentNullException.ThrowIfNull(session);

        if (TryReadByte(session, eememBaseAddress + FileOnePtrs.DiscStart, out byte fileOneByte))
        {
            if (fileOneByte == DiscSignatureByte)
            {
                activeGameFile = GameFile.FileOne;
                return true;
            }
        }

        if (TryReadByte(session, eememBaseAddress + FileTwoPtrs.DiscStart, out byte fileTwoByte))
        {
            if (fileTwoByte == DiscSignatureByte)
            {
                activeGameFile = GameFile.FileTwo;
                return true;
            }
        }

        activeGameFile = GameFile.Unknown;
        return false;
    }

    private static bool TryReadByte(IMemoryWatchSession session, nint address, out byte value)
    {
        using IMemoryWatchHandle handle = session.CreateWatch(MemoryRegionSpec.Absolute(address, 1));
        Span<byte> snapshot = stackalloc byte[1];
        if (!handle.TryReadSnapshot(snapshot, out int bytesRead) || bytesRead < 1)
        {
            value = 0;
            return false;
        }

        value = snapshot[0];
        return true;
    }
}
