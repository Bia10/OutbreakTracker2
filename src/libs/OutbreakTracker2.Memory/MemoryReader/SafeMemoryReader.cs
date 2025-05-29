using Microsoft.Extensions.Logging;
using OutbreakTracker2.WinInterop;
using System.Buffers;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace OutbreakTracker2.Memory.MemoryReader;

public sealed class SafeMemoryReader : ISafeMemoryReader
{
    private readonly ILogger<SafeMemoryReader> _logger;

    public SafeMemoryReader(ILogger<SafeMemoryReader> logger)
    {
        _logger = logger;
    }

    public T Read<T>(nint hProcess, nint address) where T : unmanaged
    {
        int size = Marshal.SizeOf<T>();
        if (size < 0)
        {
            _logger.LogError("Marshal.SizeOf<{TypeName}>() returned a negative size: {Size}. This indicates a potential issue with the type definition.", typeof(T).FullName, size);
            throw new InvalidOperationException($"Marshal.SizeOf<{typeof(T).FullName}>() returned a negative size: {size}.");
        }

        if (size == 0)
        {
            _logger.LogWarning("Attempted to read a type with zero size ({TypeName}) at address 0x{Address:X}. Returning default value.", typeof(T).FullName, address);
            return default;
        }

        byte[] buffer = ArrayPool<byte>.Shared.Rent(size);

        try
        {
            if (!SafeNativeMethods.ReadProcessMemory(hProcess, address, buffer, size, out int bytesRead))
            {
                int lastError = Marshal.GetLastWin32Error();
                _logger.LogError("Failed to read process memory for type {TypeName} at address 0x{Address:X} for process handle {ProcessHandle}. Win32 Error: 0x{Win32Error:X} ({ErrorMessage})",
                    typeof(T).FullName, address, hProcess, lastError, new Win32Exception(lastError).Message);
                throw new Win32Exception(lastError, $"Failed to read process memory at address 0x{address:X} for process handle {hProcess}.");
            }

            if (bytesRead != size)
            {
                _logger.LogError("Mismatched bytes read for type {TypeName} at address 0x{Address:X} in process handle {ProcessHandle}. Read: {BytesRead}, Expected: {ExpectedSize}",
                    typeof(T).FullName, address, hProcess, bytesRead, size);
                throw new InvalidOperationException(
                    $"Failed to read the expected number of bytes for type {typeof(T).FullName} at address 0x{address:X} in process handle {hProcess}. " +
                    $"Read: {bytesRead}, Expected: {size}");
            }

            Span<byte> bufferSpan = buffer.AsSpan(0, size);
            ref T result = ref MemoryMarshal.Cast<byte, T>(bufferSpan)[0];

            return result;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public T ReadStruct<T>(nint hProcess, nint address) where T : struct
    {
        int size = Marshal.SizeOf<T>();
        if (size < 0)
        {
            _logger.LogError("Marshal.SizeOf<{TypeName}>() returned a negative size: {Size}. This indicates a potential issue with the type definition.", typeof(T).FullName, size);
            throw new InvalidOperationException($"Marshal.SizeOf<{typeof(T).FullName}>() returned a negative size: {size}.");
        }
        if (size == 0)
        {
            _logger.LogWarning("Attempted to read a struct with zero size ({TypeName}) at address 0x{Address:X}. Returning default value.", typeof(T).FullName, address);
            return default;
        }

        byte[] buffer = ArrayPool<byte>.Shared.Rent(size);

        try
        {
            if (!SafeNativeMethods.ReadProcessMemory(hProcess, address, buffer, size, out int bytesRead))
            {
                int lastError = Marshal.GetLastWin32Error();
                _logger.LogError("Failed to read process memory for struct {TypeName} at address 0x{Address:X} for process handle {ProcessHandle}. Win32 Error: 0x{Win32Error:X} ({ErrorMessage})",
                    typeof(T).FullName, address, hProcess, lastError, new Win32Exception(lastError).Message);
                throw new Win32Exception(lastError, $"Failed to read process memory at address 0x{address:X} for process handle {hProcess}. Requested size: {size}.");
            }

            if (bytesRead != size)
            {
                _logger.LogError("Mismatched bytes read for struct {TypeName} at address 0x{Address:X} in process handle {ProcessHandle}. Read: {BytesRead}, Expected: {ExpectedSize}",
                    typeof(T).FullName, address, hProcess, bytesRead, size);
                throw new InvalidOperationException(
                    $"Failed to read the expected number of bytes for type {typeof(T).FullName} at address 0x{address:X} in process handle {hProcess}. " +
                    $"Read: {bytesRead}, Expected: {size}.");
            }

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