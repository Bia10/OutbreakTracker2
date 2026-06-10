using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using MemoryWatcher.Remote;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.LinuxInterop;
using OutbreakTracker2.Memory.SafeMemory;
using OutbreakTracker2.Memory.String;
using OutbreakTracker2.PCSX2.Client;
using OutbreakTracker2.PCSX2.EEmem;
using OutbreakTracker2.WinInterop;

namespace OutbreakTracker2.MemoryWatcherIntegration;

public sealed class OutbreakTrackerMemoryRegionCatalog : IOutbreakTrackerMemoryRegionCatalog
{
    public IReadOnlyList<OutbreakTrackerMemoryRegionDefinition> CreateRegions(nint eememBaseAddress)
    {
        return
        [
            Region("SignaturesAndRandom", eememBaseAddress + 0x0230000, 0x01C000),
            Region("ItemTablesAndWildThings", eememBaseAddress + 0x0388000, 0x012000),
            Region("PlayersScenarioDoors", eememBaseAddress + 0x0472000, 0x020000),
            Region("LobbyAndRoomState", eememBaseAddress + 0x05FF000, 0x057000),
            Region("EnemyTablesAndVirusMax", eememBaseAddress + 0x06E6000, 0x07B000),
            Region("RoomPriority", eememBaseAddress + 0x07D7000, 0x002000),
        ];
    }

    private static OutbreakTrackerMemoryRegionDefinition Region(string name, nint baseAddress, nuint byteLength) =>
        new(name, baseAddress, byteLength);
}

public sealed class MemoryWatcherSnapshotCache(
    IMemoryWatchSessionFactory sessionFactory,
    IOutbreakTrackerMemoryRegionCatalog regionCatalog,
    MemoryWatcherSettings settings,
    ILogger<MemoryWatcherSnapshotCache> logger
) : IDisposable
{
    private static readonly long RefreshIntervalTicks = Stopwatch.Frequency / 500;
    private readonly IMemoryWatchSessionFactory _sessionFactory = sessionFactory;
    private readonly IOutbreakTrackerMemoryRegionCatalog _regionCatalog = regionCatalog;
    private readonly MemoryWatcherSettings _settings = settings;
    private readonly ILogger<MemoryWatcherSnapshotCache> _logger = logger;
    private readonly Lock _sync = new();
    private IMemoryWatchSession? _session;
    private WatchedRegion[] _regions = [];

    public async ValueTask<nint> AttachAsync(IGameClient gameClient, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(gameClient);
        cancellationToken.ThrowIfCancellationRequested();

        string moduleName =
            gameClient.Process?.ProcessName
            ?? throw new InvalidOperationException("The attached game client process is unavailable.");
        IMemoryWatchSession session = _sessionFactory.Open(gameClient.Process!.Id, _settings.ToSessionOptions());

        try
        {
            ResolvedMemoryRegion eemem = session.Resolve(
                MemoryRegionSpec.SymbolRelative(
                    moduleName,
                    "EEmem",
                    dereferenceCount: 1,
                    relativeOffset: 0,
                    byteLength: 1,
                    pointerSize: 8
                )
            );

            IReadOnlyList<OutbreakTrackerMemoryRegionDefinition> regions = _regionCatalog.CreateRegions(
                eemem.BaseAddress
            );
            WatchedRegion[] watched = new WatchedRegion[regions.Count];
            try
            {
                for (int i = 0; i < regions.Count; i++)
                {
                    OutbreakTrackerMemoryRegionDefinition region = regions[i];
                    IMemoryWatchHandle handle = session.CreateWatch(
                        MemoryRegionSpec.Absolute(region.BaseAddress, region.ByteLength)
                    );
                    watched[i] = new WatchedRegion(region, handle, new byte[checked((int)region.ByteLength)]);
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

            lock (_sync)
            {
                DisposeCore();
                _session = session;
                _regions = watched;
            }

            _logger.LogInformation(
                "MemoryWatcher snapshot cache attached to PID {ProcessId} with EEmem base 0x{BaseAddress:X}. Regions: {RegionCount}",
                gameClient.Process.Id,
                eemem.BaseAddress,
                watched.Length
            );

            await ValueTask.CompletedTask.ConfigureAwait(false);
            return eemem.BaseAddress;
        }
        catch
        {
            session.Dispose();
            throw;
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
        foreach (WatchedRegion region in _regions)
        {
            region.Dispose();
        }

        _regions = [];
        _session?.Dispose();
        _session = null;
    }

    private sealed class WatchedRegion : IDisposable
    {
        private readonly OutbreakTrackerMemoryRegionDefinition _region;
        private readonly IMemoryWatchHandle _handle;
        private readonly byte[] _buffer;
        private long _lastRefreshTicks;

        public WatchedRegion(OutbreakTrackerMemoryRegionDefinition region, IMemoryWatchHandle handle, byte[] buffer)
        {
            _region = region;
            _handle = handle;
            _buffer = buffer;
        }

        public bool Contains(nint address, int byteCount)
        {
            nint endExclusive = address + byteCount;
            return address >= _region.BaseAddress && endExclusive <= _region.BaseAddress + (nint)_region.ByteLength;
        }

        public bool TryRefresh()
        {
            long now = Stopwatch.GetTimestamp();
            if (_lastRefreshTicks != 0 && now - _lastRefreshTicks <= RefreshIntervalTicks)
            {
                return true;
            }

            if (!_handle.TryReadSnapshot(_buffer, out int bytesRead) || bytesRead < _buffer.Length)
            {
                return false;
            }

            _lastRefreshTicks = now;
            return true;
        }

        public int CopyTo(nint address, Span<byte> destination)
        {
            int offset = checked((int)(address - _region.BaseAddress));
            _buffer.AsSpan(offset, destination.Length).CopyTo(destination);
            return destination.Length;
        }

        public void Dispose() => _handle.Dispose();
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

        nint current = GetAddressFromPtr(ptrOffset);
        foreach (nint offset in offsets)
        {
            long next = MemoryReader.Read<long>(_gameClient.Handle, current);
            if (next == 0)
            {
                return nint.Zero;
            }

            current = (nint)next + offset;
        }

        return current;
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
