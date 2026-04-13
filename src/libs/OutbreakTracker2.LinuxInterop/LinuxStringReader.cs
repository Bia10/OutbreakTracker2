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

    static LinuxStringReader()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public LinuxStringReader(ILogger<LinuxStringReader> logger)
    {
        _logger = logger;
    }

    public string Read(nint hProcess, nint address, Encoding? encoding = null) =>
        TryRead(hProcess, address, out string result, encoding) ? result : string.Empty;

    public bool TryRead(nint hProcess, nint address, out string result, Encoding? encoding = null)
    {
        if (address == nint.Zero)
        {
            _logger.LogWarning("Attempted to read string from NULL pointer");
            result = string.Empty;
            return true;
        }

        encoding ??= Encoding.GetEncoding(932); // Default: Shift-JIS (same as Windows impl)
        int pid = (int)hProcess;

        List<byte> bytes = new(256);
        byte[] chunk = new byte[ChunkSize];

        try
        {
            bool nullFound = false;
            while (bytes.Count < MaxSafeLength && !nullFound)
            {
                int remaining = MaxSafeLength - bytes.Count;
                int readSize = Math.Min(ChunkSize, remaining);

                int bytesRead = ReadChunk(pid, address + bytes.Count, chunk, readSize);
                if (bytesRead <= 0)
                {
                    if (bytes.Count == 0)
                    {
                        result = string.Empty;
                        return false;
                    }

                    break;
                }

                for (int i = 0; i < bytesRead; i++)
                {
                    if (chunk[i] == 0)
                    {
                        _logger.LogDebug("Null terminator found at offset {Offset}", bytes.Count + i);
                        nullFound = true;
                        break;
                    }
                    bytes.Add(chunk[i]);
                }
            }
            if (bytes.Count == 0)
            {
                result = string.Empty;
                return true;
            }

            result = ProcessFinalBytes(bytes, encoding);
            _logger.LogDebug("Read {Count} bytes: \"{Result}\"", bytes.Count, result);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during string read at 0x{Address:X}: {Message}", address, ex.Message);
            result = string.Empty;
            return false;
        }
    }

    private string ProcessFinalBytes(List<byte> bytes, Encoding encoding)
    {
        byte[] rawBytes = [.. bytes];
        string result = encoding.GetString(rawBytes);

        if (!result.Contains('\ufffd', StringComparison.Ordinal))
            return result;

        _logger.LogWarning("Encoding issues detected. Trying fallback encodings...");
        string? fallbackResult = TryFallbackEncodings(rawBytes, encoding.CodePage);
        return fallbackResult ?? result;
    }

    private string? TryFallbackEncodings(byte[] bytes, int primaryCodePage)
    {
        int[] fallbackCodePages = [Encoding.UTF8.CodePage, 932, 1252, 54936];
        HashSet<int> attemptedCodePages = [primaryCodePage];

        foreach (int codePage in fallbackCodePages)
        {
            if (!attemptedCodePages.Add(codePage))
                continue;

            try
            {
                Encoding fallbackEncoding = Encoding.GetEncoding(codePage);
                string decoded = fallbackEncoding.GetString(bytes);
                _logger.LogTrace(
                    "Fallback {EncodingName}: {Decoded} | Bytes: {Bytes}",
                    fallbackEncoding.EncodingName,
                    decoded,
                    BitConverter.ToString(bytes)
                );

                if (decoded.Contains('\ufffd', StringComparison.Ordinal))
                    continue;

                _logger.LogDebug(
                    "Recovered string using fallback encoding {EncodingName}",
                    fallbackEncoding.EncodingName
                );
                return decoded;
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Fallback code page {CodePage} failed: {Message}", codePage, ex.Message);
            }
        }

        return null;
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
