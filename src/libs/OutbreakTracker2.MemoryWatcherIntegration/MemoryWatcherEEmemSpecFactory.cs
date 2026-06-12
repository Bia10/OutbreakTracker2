using System.ComponentModel;
using System.Runtime.Versioning;
using System.Text;
using MemoryWatcher.Remote;
using Microsoft.Extensions.Logging.Abstractions;
using OutbreakTracker2.Memory.SafeMemory;
using OutbreakTracker2.PCSX2.Client;
using OutbreakTracker2.WinInterop.Structs;

namespace OutbreakTracker2.MemoryWatcherIntegration;

internal static class MemoryWatcherEEmemSpecFactory
{
    private const int EEmemPointerSizeBytes = sizeof(long);

    public static bool TryCreateEEmemExportPointerSpec(
        IGameClient gameClient,
        string moduleName,
        out MemoryRegionSpec spec
    )
    {
        ArgumentNullException.ThrowIfNull(gameClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleName);

        if (OperatingSystem.IsWindows())
        {
            if (!TryResolveEEmemExportSlotAddressWindows(gameClient, out nint exportSlotAddress))
            {
                spec = null!;
                return false;
            }

            spec = MemoryRegionSpec.Absolute(
                exportSlotAddress,
                (nuint)EEmemPointerSizeBytes,
                unitPrecision: MemoryWatchUnitPrecision.ByQWord,
                preferredElementSizeBytes: (nuint)EEmemPointerSizeBytes
            );
            return true;
        }

        spec = MemoryRegionSpec.SymbolRelative(
            moduleName,
            "EEmem",
            dereferenceCount: 0,
            relativeOffset: 0,
            byteLength: EEmemPointerSizeBytes,
            pointerSize: EEmemPointerSizeBytes,
            unitPrecision: MemoryWatchUnitPrecision.ByQWord,
            preferredElementSizeBytes: EEmemPointerSizeBytes
        );
        return true;
    }

    [SupportedOSPlatform("windows")]
    private static bool TryResolveEEmemExportSlotAddressWindows(IGameClient gameClient, out nint exportSlotAddress)
    {
        exportSlotAddress = nint.Zero;

        nint baseAddress = gameClient.MainModuleBase;
        if (baseAddress == nint.Zero)
        {
            return false;
        }

        SafeMemoryReader memoryReader = new(NullLogger<SafeMemoryReader>.Instance);
        OutbreakTracker2.Memory.String.StringReader stringReader = new(
            NullLogger<OutbreakTracker2.Memory.String.StringReader>.Instance
        );

        try
        {
            Pe32.ImageDosHeader dosHeader = memoryReader.ReadStruct<Pe32.ImageDosHeader>(
                gameClient.Handle,
                baseAddress
            );
            if (dosHeader.Magic != 0x5A4D)
            {
                return false;
            }

            nint ntHeadersAddr = baseAddress + dosHeader.LfaNew;
            Pe32.ImageNtHeaders64 ntHeaders = memoryReader.ReadStruct<Pe32.ImageNtHeaders64>(
                gameClient.Handle,
                ntHeadersAddr
            );
            if (ntHeaders.Signature != 0x00004550)
            {
                return false;
            }

            Pe32.ImageDataDirectory exportDir = ntHeaders.OptionalHeader.DataDirectory[0];
            if (exportDir.VirtualAddress == 0)
            {
                return false;
            }

            nint exportDirPtr = baseAddress + (int)exportDir.VirtualAddress;
            Pe32.ImageExportDirectory exportDirStruct = memoryReader.Read<Pe32.ImageExportDirectory>(
                gameClient.Handle,
                exportDirPtr
            );

            nint namesAddr = baseAddress + (int)exportDirStruct.AddressOfNames;
            for (int i = 0; i < exportDirStruct.NumberOfNames; i++)
            {
                nint nameRvaPtr = namesAddr + (i * 4);
                uint nameRva = memoryReader.Read<uint>(gameClient.Handle, nameRvaPtr);
                nint namePtr = baseAddress + (int)nameRva;
                string name = stringReader.Read(gameClient.Handle, namePtr, Encoding.ASCII);

                if (!name.Equals("EEmem", StringComparison.Ordinal))
                {
                    continue;
                }

                nint ordinalsAddr = baseAddress + (int)exportDirStruct.AddressOfNameOrdinals;
                ushort ordinal = memoryReader.Read<ushort>(gameClient.Handle, ordinalsAddr + (i * 2));

                nint functionsAddr = baseAddress + (int)exportDirStruct.AddressOfFunctions;
                nint functionRvaPtr = functionsAddr + (ordinal * 4);
                uint functionRva = memoryReader.Read<uint>(gameClient.Handle, functionRvaPtr);

                exportSlotAddress = baseAddress + (int)functionRva;
                return exportSlotAddress != nint.Zero;
            }
        }
        catch (Exception ex) when (ex is System.IO.IOException or InvalidOperationException or Win32Exception) { }

        return false;
    }
}
