using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using OutbreakTracker2.Memory.String;
using MemoryStringReader = OutbreakTracker2.Memory.String.StringReader;

namespace OutbreakTracker2.UnitTests;

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
            IStringReader reader = CreateReader();
            nint processHandle = GetCurrentProcessHandle(currentProcess);

            bool success = reader.TryRead(processHandle, address, out string result, Encoding.UTF8);

            await Assert.That(success).IsTrue();
            await Assert.That(result).IsEqualTo("\u30FB");
            await Assert.That(result.Contains('\ufffd')).IsFalse();
        }
        finally
        {
            Marshal.FreeHGlobal(address);
        }
    }

    private static IStringReader CreateReader()
    {
        if (OperatingSystem.IsWindows())
            return CreateWindowsReader();

        if (OperatingSystem.IsLinux())
        {
            Type readerType =
                Type.GetType("OutbreakTracker2.LinuxInterop.LinuxStringReader, OutbreakTracker2.LinuxInterop")
                ?? throw new InvalidOperationException("Linux string reader type could not be loaded.");
            Type loggerType = typeof(NullLogger<>).MakeGenericType(readerType);
            object logger =
                loggerType.GetProperty("Instance")?.GetValue(null)
                ?? throw new InvalidOperationException("Linux string reader logger could not be created.");

            return (IStringReader)(
                Activator.CreateInstance(readerType, logger)
                ?? throw new InvalidOperationException("Linux string reader could not be created.")
            );
        }

        throw new PlatformNotSupportedException(
            "Only Windows and Linux are currently supported by string reader tests."
        );
    }

    [SupportedOSPlatform("windows")]
    private static IStringReader CreateWindowsReader() =>
        new MemoryStringReader(NullLogger<MemoryStringReader>.Instance);

    private static nint GetCurrentProcessHandle(Process currentProcess) =>
        OperatingSystem.IsLinux() ? currentProcess.Id : currentProcess.Handle;
}
