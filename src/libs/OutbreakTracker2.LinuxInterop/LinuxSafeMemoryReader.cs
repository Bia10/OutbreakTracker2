using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Memory.SafeMemory;

namespace OutbreakTracker2.LinuxInterop;

/// <summary>
/// Linux implementation of <see cref="ISafeMemoryReader"/> using <c>process_vm_readv</c>(2).
/// The <c>hProcess</c> parameter is treated as the target PID cast to <see cref="nint"/>.
/// </summary>
[SupportedOSPlatform("linux")]
public sealed class LinuxSafeMemoryReader(ILogger<LinuxSafeMemoryReader> logger) : ISafeMemoryReader
{
    private readonly ILogger<LinuxSafeMemoryReader> _logger = logger;

    public T Read<T>(nint hProcess, nint address)
        where T : unmanaged
    {
        int size = Marshal.SizeOf<T>();
        if (size == 0)
        {
            _logger.LogWarning(
                "Attempted to read a type with zero size ({TypeName}) at address 0x{Address:X}. Returning default value.",
                typeof(T).FullName,
                address
            );
            return default;
        }

        byte[] buffer = ArrayPool<byte>.Shared.Rent(size);
        try
        {
            ReadIntoBuffer(hProcess, address, buffer, size, typeof(T).FullName);

            Span<byte> bufferSpan = buffer.AsSpan(0, size);
            return MemoryMarshal.Cast<byte, T>(bufferSpan)[0];
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
        if (size == 0)
        {
            _logger.LogWarning(
                "Attempted to read a struct with zero size ({TypeName}) at address 0x{Address:X}. Returning default value.",
                typeof(T).FullName,
                address
            );
            return default;
        }

        byte[] buffer = ArrayPool<byte>.Shared.Rent(size);
        try
        {
            ReadIntoBuffer(hProcess, address, buffer, size, typeof(T).FullName);

            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            }
            finally
            {
                if (handle.IsAllocated)
                    handle.Free();
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private unsafe void ReadIntoBuffer(nint hProcess, nint address, byte[] buffer, int size, string? typeName)
    {
        fixed (byte* ptr = buffer)
        {
            Iovec localIov = new() { iov_base = (nint)ptr, iov_len = (nuint)size };
            Iovec remoteIov = new() { iov_base = address, iov_len = (nuint)size };

            long bytesRead = LinuxNativeMethods.ProcessVmReadv((int)hProcess, ref localIov, 1, ref remoteIov, 1, 0);

            if (bytesRead < 0)
            {
                int errno = Marshal.GetLastPInvokeError();
                string errorMessage = Marshal.GetLastPInvokeErrorMessage();
                _logger.LogError(
                    "process_vm_readv failed for type {TypeName} at address 0x{Address:X} (PID {Pid}). errno: {Errno} ({Message})",
                    typeName,
                    address,
                    (int)hProcess,
                    errno,
                    errorMessage
                );
                throw new IOException(
                    $"process_vm_readv failed at address 0x{address:X} for PID {(int)hProcess}. errno: {errno} ({errorMessage})"
                );
            }

            if (bytesRead != size)
            {
                _logger.LogError(
                    "process_vm_readv partial read for type {TypeName} at address 0x{Address:X} (PID {Pid}). Read: {BytesRead}, Expected: {ExpectedSize}",
                    typeName,
                    address,
                    (int)hProcess,
                    bytesRead,
                    size
                );
                throw new InvalidOperationException(
                    $"Partial read for {typeName} at 0x{address:X}: got {bytesRead} bytes, expected {size}."
                );
            }
        }
    }
}
