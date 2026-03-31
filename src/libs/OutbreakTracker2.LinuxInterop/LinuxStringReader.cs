using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Memory.String;

namespace OutbreakTracker2.LinuxInterop;

/// <summary>
/// Linux implementation of <see cref="IStringReader"/> using <c>process_vm_readv</c>(2).
/// Reads in 256-byte chunks to amortize syscall overhead, scanning for null terminator.
/// The <c>hProcess</c> parameter is treated as the target PID cast to <see cref="nint"/>.
/// </summary>
[SupportedOSPlatform("linux")]
public sealed class LinuxStringReader : IStringReader
{
    private const int ChunkSize = 256;
    private const int MaxSafeLength = 1048576;

    private readonly ILogger<LinuxStringReader> _logger;

    public LinuxStringReader(ILogger<LinuxStringReader> logger)
    {
        _logger = logger;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public string Read(nint hProcess, nint address, Encoding? encoding = null)
    {
        if (address == nint.Zero)
        {
            _logger.LogWarning("Attempted to read string from NULL pointer");
            return string.Empty;
        }

        encoding ??= Encoding.GetEncoding(932); // Default: Shift-JIS (same as Windows impl)
        int pid = (int)hProcess;

        List<byte> bytes = new(256);
        byte[] chunk = new byte[ChunkSize];

        try
        {
            while (bytes.Count < MaxSafeLength)
            {
                int remaining = MaxSafeLength - bytes.Count;
                int readSize = Math.Min(ChunkSize, remaining);

                int bytesRead = ReadChunk(pid, address + bytes.Count, chunk, readSize);
                if (bytesRead <= 0)
                    break;

                for (int i = 0; i < bytesRead; i++)
                {
                    if (chunk[i] == 0)
                    {
                        _logger.LogDebug("Null terminator found at offset {Offset}", bytes.Count + i);
                        goto done;
                    }
                    bytes.Add(chunk[i]);
                }
            }

            done:
            if (bytes.Count == 0)
                return string.Empty;

            string result = encoding.GetString([.. bytes]);
            _logger.LogDebug("Read {Count} bytes: \"{Result}\"", bytes.Count, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during string read at 0x{Address:X}: {Message}", address, ex.Message);
            return string.Empty;
        }
    }

    private unsafe int ReadChunk(int pid, nint address, byte[] buffer, int size)
    {
        fixed (byte* ptr = buffer)
        {
            Iovec localIov = new() { iov_base = (nint)ptr, iov_len = (nuint)size };
            Iovec remoteIov = new() { iov_base = address, iov_len = (nuint)size };

            long result = LinuxNativeMethods.ProcessVmReadv(pid, ref localIov, 1, ref remoteIov, 1, 0);
            if (result < 0)
            {
                int errno = Marshal.GetLastPInvokeError();
                _logger.LogWarning(
                    "process_vm_readv failed at 0x{Address:X} (PID {Pid}). errno: {Errno} ({Message})",
                    address,
                    pid,
                    errno,
                    new Win32Exception(errno).Message
                );
                return 0;
            }

            return (int)result;
        }
    }
}
