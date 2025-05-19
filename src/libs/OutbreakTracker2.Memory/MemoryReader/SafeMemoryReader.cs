using OutbreakTracker2.WinInterop;
using System.Buffers;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace OutbreakTracker2.Memory.MemoryReader;

public sealed class SafeMemoryReader : ISafeMemoryReader
{
    public T Read<T>(nint hProcess, nint address) where T : unmanaged
    {
        int size = Marshal.SizeOf<T>();
        switch (size)
        {
            case < 0: throw new InvalidOperationException($"Marshal.SizeOf<{typeof(T).FullName}>() returned a negative size: {size}.");
            case 0: return default;
            default:
                {
                    byte[] buffer = ArrayPool<byte>.Shared.Rent(size);

                    try
                    {
                        if (!SafeNativeMethods.ReadProcessMemory(hProcess, address, buffer, size, out int bytesRead))
                            throw new Win32Exception(Marshal.GetLastWin32Error(),
                                $"Failed to read process memory at address {address} for process handle {hProcess}.");

                        if (bytesRead != size)
                            throw new InvalidOperationException(
                                $"Failed to read the expected number of bytes for type {typeof(T).FullName} at address {address} in process handle {hProcess}." +
                                $" Read: {bytesRead}, Expected: {size}");

                        Span<byte> bufferSpan = buffer.AsSpan(0, size);
                        ref T result = ref MemoryMarshal.Cast<byte, T>(bufferSpan)[0];

                        return result;
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                }
        }
    }

    public T ReadStruct<T>(nint hProcess, nint address) where T : struct
    {
        int size = Marshal.SizeOf<T>();
        switch (size)
        {
            case < 0: throw new InvalidOperationException($"Marshal.SizeOf<{typeof(T).FullName}>() returned a negative size: {size}.");
            case 0: return default;
            default:
                {
                    byte[] buffer = ArrayPool<byte>.Shared.Rent(size);

                    try
                    {
                        if (!SafeNativeMethods.ReadProcessMemory(hProcess, address, buffer, size, out int bytesRead))
                            throw new Win32Exception(Marshal.GetLastWin32Error(),
                                $"Failed to read process memory at address 0x{address:X} for process handle {hProcess}. Requested size: {size}.");

                        if (bytesRead != size)
                            throw new InvalidOperationException(
                                $"Failed to read the expected number of bytes for type {typeof(T).FullName} at address 0x{address:X} in process handle {hProcess}. " +
                                $"Read: {bytesRead}, Expected: {size}.");

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
        }
    }
}