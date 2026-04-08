using System.Buffers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using OutbreakTracker2.WinInterop;

namespace OutbreakTracker2.Memory.UnsafeMemory;

// TODO: Well since the read size is always within stackAlloc 8kb buffer this indeed is a bit faster than safe version
// however the limitations and potential instability introduced is not yet worth it, leaving here for later optimizations
[SupportedOSPlatform("windows")]
public unsafe class UnsafeMemoryReader : IUnsafeMemoryReader
{
    private const int StackAllocThreshold = 8192;

    public T Read<T>(nint hProcess, nint address)
        where T : unmanaged
    {
        int size = Unsafe.SizeOf<T>();
        if (size == 0)
            return default;

        byte[]? arrayPoolBuffer = null;

        try
        {
            byte* bufferPtr;
            if (size <= StackAllocThreshold)
            {
                byte* stackAllocatedBuffer = stackalloc byte[size];
                bufferPtr = stackAllocatedBuffer;

                if (!UnsafeNativeMethods.ReadProcessMemory(hProcess, address, bufferPtr, size, out int bytesRead))
                    throw new Win32Exception(Marshal.GetLastPInvokeError());
                if (bytesRead != size)
                    throw new InvalidOperationException(
                        $"Failed to read the expected number of bytes. Read: {bytesRead}, Expected: {size}"
                    );

                return *(T*)bufferPtr;
            }
            else
            {
                arrayPoolBuffer = ArrayPool<byte>.Shared.Rent(size);

                fixed (byte* pinnedPtr = arrayPoolBuffer)
                {
                    bufferPtr = pinnedPtr;

                    if (!UnsafeNativeMethods.ReadProcessMemory(hProcess, address, bufferPtr, size, out int bytesRead))
                        throw new Win32Exception(Marshal.GetLastPInvokeError());
                    if (bytesRead != size)
                        throw new InvalidOperationException(
                            $"Failed to read the expected number of bytes. Read: {bytesRead}, Expected: {size}"
                        );

                    return *(T*)bufferPtr;
                }
            }
        }
        finally
        {
            if (arrayPoolBuffer is not null)
                ArrayPool<byte>.Shared.Return(arrayPoolBuffer);
        }
    }
}
