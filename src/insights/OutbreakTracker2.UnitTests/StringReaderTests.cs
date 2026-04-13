using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using MemoryStringReader = OutbreakTracker2.Memory.String.StringReader;

namespace OutbreakTracker2.UnitTests;

[SupportedOSPlatform("windows")]
public sealed class StringReaderTests
{
    [Test]
    public async Task TryRead_UsesCleanFallback_WhenPrimaryEncodingProducesReplacementCharacters()
    {
        byte[] bytes = [0x81, 0x00];
        nint address = Marshal.AllocHGlobal(bytes.Length);

        try
        {
            Marshal.Copy(bytes, 0, address, bytes.Length);

            using Process currentProcess = Process.GetCurrentProcess();
            MemoryStringReader reader = new(NullLogger<MemoryStringReader>.Instance);

            bool success = reader.TryRead(currentProcess.Handle, address, out string result, Encoding.UTF8);

            await Assert.That(success).IsTrue();
            await Assert.That(result).IsEqualTo("\u30FB");
            await Assert.That(result.Contains('\ufffd')).IsFalse();
        }
        finally
        {
            Marshal.FreeHGlobal(address);
        }
    }
}
