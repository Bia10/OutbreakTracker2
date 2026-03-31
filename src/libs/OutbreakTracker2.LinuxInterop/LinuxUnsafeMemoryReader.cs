using System.Buffers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using OutbreakTracker2.Memory.UnsafeMemory;

namespace OutbreakTracker2.LinuxInterop;

/// <summary>
/// Linux implementation of <see cref="IUnsafeMemoryReader"/> using <c>process_vm_readv</c>(2).
/// The <c>hProcess</c> parameter is treated as the target PID cast to <see cref="nint"/>.
/// </summary>
[SupportedOSPlatform("linux")]
public unsafe class LinuxUnsafeMemoryReader : IUnsafeMemoryReader
{
    private const int StackAllocThreshold = 8192;

    public T Read<T>(nint hProcess, nint address)
        where T : unmanaged
    {
        int size = Unsafe.SizeOf<T>();
        return size switch
        {
            < 0 => throw new InvalidOperationException($"Size of T cannot be negative. Size: {size}"),
            0 => default,
            _ => ReadCore<T>((int)hProcess, address, size),
        };
    }

    private static T ReadCore<T>(int pid, nint address, int size)
        where T : unmanaged
    {
        byte[]? arrayPoolBuffer = null;
        try
        {
            if (size <= StackAllocThreshold)
            {
                byte* stackBuffer = stackalloc byte[size];
                ProcessVmRead(pid, address, stackBuffer, size);
                return *(T*)stackBuffer;
            }

            arrayPoolBuffer = ArrayPool<byte>.Shared.Rent(size);
            fixed (byte* pinnedPtr = arrayPoolBuffer)
            {
                ProcessVmRead(pid, address, pinnedPtr, size);
                return *(T*)pinnedPtr;
            }
        }
        finally
        {
            if (arrayPoolBuffer is not null)
                ArrayPool<byte>.Shared.Return(arrayPoolBuffer);
        }
    }

    private static void ProcessVmRead(int pid, nint address, byte* localBuffer, int size)
    {
        Iovec localIov = new() { iov_base = (nint)localBuffer, iov_len = (nuint)size };
        Iovec remoteIov = new() { iov_base = address, iov_len = (nuint)size };

        long bytesRead = LinuxNativeMethods.ProcessVmReadv(pid, ref localIov, 1, ref remoteIov, 1, 0);

        if (bytesRead < 0)
            throw new Win32Exception(Marshal.GetLastPInvokeError());

        if (bytesRead != size)
            throw new InvalidOperationException(
                $"process_vm_readv partial read: got {bytesRead} bytes, expected {size}."
            );
    }
}
