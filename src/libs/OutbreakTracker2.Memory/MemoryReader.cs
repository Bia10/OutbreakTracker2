﻿using OutbreakTracker2.WinInterop;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace OutbreakTracker2.Memory;

public class MemoryReader
{
    public static T Read<T>(nint hProcess, nint address) where T : struct
    {
        int size = Marshal.SizeOf<T>();
        byte[] buffer = new byte[size];
        NativeMethods.ReadProcessMemory(hProcess, address, buffer, buffer.Length, out _);
        GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

        try
        {
            return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        } 
    }

    // This has to be a bit overcomplicated as Outbreak supports multiple char widths like halfwidth and fullwidth character forms
    public static string ReadString(nint hProcess, nint address, Encoding? encoding = null)
    {
        // Under some bizzare scenario, if the null terminator is not found, we can read up to MAX of 1MB of data
        const int maxSafeLength = 1048576;
        Console.WriteLine($"Starting ReadString at address 0x{address:X8}");

        if (address == 0)
        {
            Console.WriteLine("Attempted to read from NULL pointer");
            return string.Empty;
        }

        // TODO: move to app
        // Register the encoding provider if not already registered
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // Default to Shift-JIS for Japanese games with fullwidth Latin
        encoding ??= Encoding.GetEncoding(932);
        var bytes = new List<byte>(256);
        var buffer = new byte[1];
        int consecutiveFails = 0;
        string result = string.Empty;

        try
        {
            while (bytes.Count < maxSafeLength)
            {
                bool success = NativeMethods.ReadProcessMemory(hProcess, address + bytes.Count, buffer,1, out int bytesRead);

                if (!HandleReadResult(ref consecutiveFails, success, bytes.Count, bytesRead, address, out bool shouldBreak))
                    if (shouldBreak) break;

                if (buffer[0] == 0)
                {
                    Console.WriteLine($"Null terminator found at offset {bytes.Count}");
                    break;
                }

                bytes.Add(buffer[0]);
                LogByteDetails(buffer[0], bytes.Count - 1, bytes, encoding);
            }

            result = ProcessFinalBytes(bytes, encoding);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Critical error during memory read: {ex.Message}");
            result = string.Empty;
        }

        return result;
    }

    private static void LogByteDetails(byte b, int offset, List<byte> bytes, Encoding encoding)
    {
        var multiByteInfo = new StringBuilder();
        if (encoding.CodePage == 932 && offset > 0 && IsShiftJisLeadByte(bytes[offset - 1]))
        {
            try 
            {
                var pair = new[] { bytes[offset - 1], b };
                string decoded = encoding.GetString(pair);
                multiByteInfo.Append($" | Shift-JIS Pair: {decoded} (0x{bytes[offset - 1]:X2}{b:X2})");
            }
            catch { /* Ignore decoding errors */ }
        }

        Console.WriteLine($"Byte 0x{b:X2} @ {offset} - {GetByteInterpretations(b)}{multiByteInfo}");
    }

    private static bool IsShiftJisLeadByte(byte @byte)
    {
        return (@byte >= 0x81 && @byte <= 0x9F) || (@byte >= 0xE0 && @byte <= 0xEF);
    }

    private static string ProcessFinalBytes(List<byte> bytes, Encoding encoding)
    {
        if (bytes.Count == 0)
        {
            Console.WriteLine("Empty string read from memory");
            return string.Empty;
        }

        string result = encoding.GetString(bytes.ToArray());

        if (result.Contains('\ufffd'))
        {
            Console.WriteLine($"Encoding issues detected. Trying fallback encodings...");
            TryFallbackEncodings(bytes);
        }

        Console.WriteLine($"Read {bytes.Count} bytes: \"{result}\"");
        return result;
    }

    private static void TryFallbackEncodings(List<byte> bytes)
    {
        var encodings = new[]
        {
            Encoding.UTF8,
            Encoding.GetEncoding(932), // Shift-JIS
            Encoding.GetEncoding(1252), // Western European
            Encoding.GetEncoding(54936) // GB18030 (Chinese fallback)
        };

        foreach (var encoding in encodings)
        {
            try
            {
                string decoded = encoding.GetString([.. bytes]);
                Console.WriteLine($"Fallback {encoding.EncodingName}: {decoded} | Bytes: {BitConverter.ToString([.. bytes])}");
            }
            catch 
            { 
                /* Ignore unsupported encodings */
            }
        }
    }

    private static bool HandleReadResult(ref int consecutiveFails, bool success, int offset, int bytesRead, nint address, out bool shouldBreak)
    {
        shouldBreak = false;
        if (!success)
        {
            int lastError = Marshal.GetLastWin32Error();
            Console.WriteLine($"ReadProcessMemory failed at offset {offset} " +
                        $"(Address: 0x{(address + offset):X8}) " +
                        $"Error: 0x{lastError:X8} ({new Win32Exception(lastError).Message})");
            consecutiveFails++;
        
            if (consecutiveFails >= 3)
            {
                Console.WriteLine($"Aborting read after 3 consecutive failures");
                shouldBreak = true;
            }
            return false;
        }

        consecutiveFails = 0;

        if (bytesRead != 1)
        {
            Console.WriteLine($"Partial read at offset {offset} Requested: 1, Got: {bytesRead}");
            return false;
        }

        return true;
    }

    private static string GetByteInterpretations(byte @byte)
    {
        var sb = new StringBuilder();
    
        try
        {
            sb.Append($"ASCII: {FormatChar(Encoding.ASCII, @byte)} ");
            sb.Append($"UTF-8: {FormatChar(Encoding.UTF8, @byte)} ");
            sb.Append($"Win1252: {FormatChar(Encoding.GetEncoding(1252), @byte)} ");
            sb.Append($"Latin1: {FormatChar(Encoding.GetEncoding(28591), @byte)}");
        }
        catch (Exception ex)
        {
            sb.Append($"Encoding error: {ex.Message}");
        }

        return sb.ToString();
    }

    private static string FormatChar(Encoding encoding, byte @byte)
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
                _ when char.IsControl(@char) => $"\\u{(int)@char:X4}",
                _ when @char == '\ufffd' => "�",
                _ => @char.ToString()
            };
        }
        catch
        {
            return "�";
        }
    }
}
