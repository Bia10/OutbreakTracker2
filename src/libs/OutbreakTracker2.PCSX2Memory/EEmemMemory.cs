using OutbreakTracker2.Memory;
using OutbreakTracker2.WinInterop;
using OutbreakTracker2.WinInterop.Structs;
using System.ComponentModel;
using System.Diagnostics;

namespace OutbreakTracker2.PCSX2Memory;

public class EEmemMemory
{
    public readonly nint EEmemBaseAddress = 0;

    public EEmemMemory(string processName)
    {
        EEmemBaseAddress = GetEEmemBaseAddress(processName);
        if (EEmemBaseAddress == 0)
            throw new Exception("Failed to find EEmem base address.");
    }

    public static nint GetEEmemBaseAddress(string processName)
    {
        Process[] processes = Process.GetProcessesByName(processName);
        if (processes.Length == 0) return 0;

        Process process = processes[0];
        nint hProcess = NativeMethods.OpenProcess(ProcessAccessFlags.VmRead | ProcessAccessFlags.QueryInformation, false, process.Id);
        if (hProcess == nint.Zero) throw new Win32Exception();

        try
        {
            nint baseAddress = process.MainModule?.BaseAddress ?? throw new InvalidOperationException("MainModule or BaseAddress is null.");

            // Read DOS Header
            PE32.ImageDosHeader dosHeader = MemoryReader.Read<PE32.ImageDosHeader>(hProcess, baseAddress);
            if (dosHeader.Magic != 0x5A4D) // "MZ"
                return nint.Zero;

            // Read NT Headers
            nint ntHeadersAddr = baseAddress + dosHeader.LfaNew;
            PE32.ImageNtHeaders64 ntHeaders = MemoryReader.Read<PE32.ImageNtHeaders64>(hProcess, ntHeadersAddr);
            if (ntHeaders.Signature != 0x00004550) // "PE\0\0"
                return nint.Zero;

            // Get Export Directory
            PE32.ImageDataDirectory exportDir = ntHeaders.OptionalHeader.DataDirectory[0];
            if (exportDir.VirtualAddress == 0)
                return nint.Zero;

            nint exportDirPtr = baseAddress + (int)exportDir.VirtualAddress;
            PE32.ImageExportDirectory exportDirStruct = MemoryReader.Read<PE32.ImageExportDirectory>(hProcess, exportDirPtr);

            // Read Export Names
            nint namesAddr = baseAddress + (int)exportDirStruct.AddressOfNames;
            for (int i = 0; i < exportDirStruct.NumberOfNames; i++)
            {
                nint nameRvaPtr = namesAddr + i * 4;
                uint nameRva = MemoryReader.Read<uint>(hProcess, nameRvaPtr);
                nint namePtr = baseAddress + (int)nameRva;
                string name = MemoryReader.ReadString(hProcess, namePtr);

                if (name.Equals("EEmem", StringComparison.Ordinal))
                {
                    // Get corresponding ordinal and function address
                    nint ordinalsAddr = baseAddress + (int)exportDirStruct.AddressOfNameOrdinals;
                    ushort ordinal = MemoryReader.Read<ushort>(hProcess, ordinalsAddr + i * 2);

                    nint functionsAddr = baseAddress + (int)exportDirStruct.AddressOfFunctions;
                    nint functionRvaPtr = functionsAddr + ordinal * 4;
                    uint functionRva = MemoryReader.Read<uint>(hProcess, functionRvaPtr);

                    // Read the EEMem pointer (64-bit)
                    nint eememAddr = baseAddress + (int)functionRva;
                    long eememValue = MemoryReader.Read<long>(hProcess, eememAddr);
                    return (nint)eememValue;
                }
            }

            return 0;
        }
        finally
        {
            NativeMethods.CloseHandle(hProcess);
        }
    }
}
