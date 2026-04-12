using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.WinInterop;

namespace OutbreakTracker2.Memory.String;

[SupportedOSPlatform("windows")]
public sealed class StringReader : IStringReader
{
    private readonly ILogger<StringReader> _logger;

    static StringReader()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public StringReader(ILogger<StringReader> logger)
    {
        _logger = logger;
    }

    public string Read(nint hProcess, nint address, Encoding? encoding = null) =>
        TryRead(hProcess, address, out string result, encoding) ? result : string.Empty;

    // This has to be a bit overcomplicated as Outbreak supports multiple char widths like halfwidth and fullwidth character forms
    public bool TryRead(nint hProcess, nint address, out string result, Encoding? encoding = null)
    {
        // Under some bizzare scenario, if the null terminator is not found, we can read up to MAX of 1MB of data
        const int maxSafeLength = 1048576;
        // Read this many bytes per ReadProcessMemory call to amortise syscall overhead.
        const int chunkSize = 256;

        if (address == nint.Zero)
        {
            _logger.LogWarning("Attempted to read from NULL pointer");
            result = string.Empty;
            return true;
        }

        // Default to Shift-JIS for Japanese games with fullwidth Latin
        encoding ??= Encoding.GetEncoding(932);
        List<byte> bytes = new(chunkSize);
        byte[] chunk = new byte[chunkSize];
        int consecutiveFails = 0;
        try
        {
            while (bytes.Count < maxSafeLength)
            {
                int toRead = Math.Min(chunkSize, maxSafeLength - bytes.Count);
                bool success = SafeNativeMethods.ReadProcessMemory(
                    hProcess,
                    address + bytes.Count,
                    chunk,
                    toRead,
                    out int bytesRead
                );

                if (!success || bytesRead == 0)
                {
                    consecutiveFails++;
                    int lastError = Marshal.GetLastPInvokeError();
                    _logger.LogWarning(
                        "ReadProcessMemory failed at offset {Offset} (Address: 0x{Address:X8}) Error: 0x{LastError:X8} ({Message})",
                        bytes.Count,
                        address + bytes.Count,
                        lastError,
                        new System.ComponentModel.Win32Exception(lastError).Message
                    );

                    if (consecutiveFails >= 3)
                    {
                        _logger.LogError("Aborting read after 3 consecutive failures");

                        if (bytes.Count == 0)
                        {
                            result = string.Empty;
                            return false;
                        }

                        break;
                    }

                    continue;
                }

                consecutiveFails = 0;

                bool nullFound = false;
                for (int i = 0; i < bytesRead; i++)
                {
                    if (chunk[i] is 0)
                    {
                        nullFound = true;
                        break;
                    }

                    bytes.Add(chunk[i]);
                    LogByteDetails(chunk[i], bytes.Count - 1, bytes, encoding);
                }

                if (nullFound)
                    break;
            }

            result = ProcessFinalBytes(bytes, encoding);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during memory read: {ExMessage}", ex.Message);
            result = string.Empty;
            return false;
        }
    }

    private void LogByteDetails(byte @byte, int offset, List<byte> bytes, Encoding encoding)
    {
        StringBuilder multiByteInfo = new();
        if (encoding.CodePage is 932 && offset > 0 && IsShiftJisLeadByte(bytes[offset - 1]))
            try
            {
                byte[] pair = [bytes[offset - 1], @byte];
                string decoded = encoding.GetString(pair);
                multiByteInfo.Append($" | Shift-JIS Pair: {decoded} (0x{bytes[offset - 1]:X2}{@byte:X2})");
            }
            catch (DecoderFallbackException ex)
            {
                multiByteInfo.Append($" | Shift-JIS Pair: 0x{bytes[offset - 1]:X2}{@byte:X2} (Decoding Error)");
                _logger.LogTrace(ex, "Shift-JIS Pair Decoding Error: {ExMessage}", ex.Message);
            }

        _logger.LogTrace(
            "Byte 0x{B:X2} @ {Offset} - {S}{MultiByteInfo}",
            @byte,
            offset,
            GetByteInterpretations(@byte),
            multiByteInfo
        );
    }

    private static bool IsShiftJisLeadByte(byte @byte) => @byte is >= 0x81 and <= 0x9F or >= 0xE0 and <= 0xEF;

    private string ProcessFinalBytes(List<byte> bytes, Encoding encoding)
    {
        if (bytes.Count is 0)
        {
            _logger.LogTrace("Empty string read from memory");
            return string.Empty;
        }

        string result = encoding.GetString([.. bytes]);

        if (result.Contains('\ufffd', StringComparison.Ordinal))
        {
            _logger.LogWarning("Encoding issues detected. Trying fallback encodings...");
            TryFallbackEncodings(bytes);
        }

        return result;
    }

    private void TryFallbackEncodings(List<byte> bytes)
    {
        Encoding[] encodings =
        [
            Encoding.UTF8,
            Encoding.GetEncoding(932), // Shift-JIS
            Encoding.GetEncoding(1252), // Western European
            Encoding.GetEncoding(54936), // GB18030 (Chinese fallback)
        ];

        foreach (Encoding encoding in encodings)
            try
            {
                string decoded = encoding.GetString([.. bytes]);
                _logger.LogTrace(
                    $"Fallback {encoding.EncodingName}: {decoded} | Bytes: {BitConverter.ToString([.. bytes])}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogTrace(
                    ex,
                    "Fallback {EncodingEncodingName} failed: {ExMessage}",
                    encoding.EncodingName,
                    ex.Message
                );
            }
    }

    private string GetByteInterpretations(byte @byte)
    {
        StringBuilder sb = new();

        try
        {
            sb.Append($"ASCII: {FormatChar(Encoding.ASCII, @byte)} ");
            sb.Append($"UTF-8: {FormatChar(Encoding.UTF8, @byte)} ");
            sb.Append($"Win1252: {FormatChar(Encoding.GetEncoding(1252), @byte)} ");
            sb.Append($"Latin1: {FormatChar(Encoding.GetEncoding(28591), @byte)}");
        }
        catch (Exception ex)
        {
            sb.Append($"Encoding error: {ex}");
            _logger.LogError(ex, "Encoding error: {ExMessage}", ex.Message);
        }

        return sb.ToString();
    }

    private string FormatChar(Encoding encoding, byte @byte)
    {
        try
        {
            char @char = encoding.GetChars([@byte])[0];
            return @char switch
            {
                '\u0000' => "\\0",
                '\t' => "\\t",
                '\n' => "\\n",
                '\r' => "\\r",
                '\ufffd' => "�",
                _ when char.IsControl(@char) => $"\\u{(int)@char:X4}",
                _ => @char.ToString(),
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to format byte 0x{@Byte:X2} as character", @byte);
            _logger.LogTrace(ex, "Encoding error: {ExMessage}", ex.Message);
            return "�";
        }
    }
}
