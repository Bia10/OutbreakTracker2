namespace OutbreakTracker2.WinInterop;

internal static class PE32
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ImageDosHeader
    {
        public ushort Magic;       // Magic number (0x5A4D)
        public ushort Cblp;        // Bytes on last page of file
        public ushort Cp;          // Pages in file
        public ushort CrLc;        // Relocations
        public ushort CparHdr;     // Size of header in paragraphs
        public ushort MinAlloc;    // Minimum extra paragraphs needed
        public ushort MaxAlloc;    // Maximum extra paragraphs needed
        public ushort Ss;          // Initial (relative) SS value
        public ushort Sp;          // Initial SP value
        public ushort Csum;        // Checksum
        public ushort Ip;          // Initial IP value
        public ushort Cs;          // Initial (relative) CS value
        public ushort LfaRlc;      // File address of relocation table
        public ushort Ovno;        // Overlay number
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] Res;       // Reserved words
        public ushort OemId;       // OEM identifier
        public ushort OemInfo;     // OEM information
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public ushort[] Res2;      // Reserved words
        public int LfaNew;         // File address of new exe header
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ImageNtHeaders64
    {
        public uint Signature;
        public ImageFileHeader FileHeader;
        public ImageOptionalHeader64 OptionalHeader;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ImageFileHeader
    {
        public ushort Machine;
        public ushort NumberOfSections;
        public uint TimeDateStamp;
        public uint PointerToSymbolTable;
        public uint NumberOfSymbols;
        public ushort SizeOfOptionalHeader;
        public ushort Characteristics;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ImageOptionalHeader64
    {
        public ushort Magic; // 0x20B for PE32+
        public byte MajorLinkerVersion;
        public byte MinorLinkerVersion;
        public uint SizeOfCode;
        public uint SizeOfInitializedData;
        public uint SizeOfUninitializedData;
        public uint AddressOfEntryPoint;
        public uint BaseOfCode;
        public ulong ImageBase;
        public uint SectionAlignment;
        public uint FileAlignment;
        public ushort MajorOperatingSystemVersion;
        public ushort MinorOperatingSystemVersion;
        public ushort MajorImageVersion;
        public ushort MinorImageVersion;
        public ushort MajorSubsystemVersion;
        public ushort MinorSubsystemVersion;
        public uint Win32VersionValue;
        public uint SizeOfImage;
        public uint SizeOfHeaders;
        public uint CheckSum;
        public ushort Subsystem;
        public ushort DllCharacteristics;
        public ulong SizeOfStackReserve;
        public ulong SizeOfStackCommit;
        public ulong SizeOfHeapReserve;
        public ulong SizeOfHeapCommit;
        public uint LoaderFlags;
        public uint NumberOfRvaAndSizes;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public ImageDataDirectory[] DataDirectory;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ImageDataDirectory
    {
        public uint VirtualAddress;
        public uint Size;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ImageExportDirectory
    {
        public uint Characteristics;
        public uint TimeDateStamp;
        public ushort MajorVersion;
        public ushort MinorVersion;
        public uint Name;
        public uint Base;
        public uint NumberOfFunctions;
        public uint NumberOfNames;
        public uint AddressOfFunctions;
        public uint AddressOfNames;
        public uint AddressOfNameOrdinals;
    }
}