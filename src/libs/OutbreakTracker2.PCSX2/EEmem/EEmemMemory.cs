using System.Globalization;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Memory.SafeMemory;
using OutbreakTracker2.Memory.String;
using OutbreakTracker2.PCSX2.Client;
using OutbreakTracker2.WinInterop.Structs;

namespace OutbreakTracker2.PCSX2.EEmem;

public sealed class EEmemMemory(ISafeMemoryReader memoryReader, IStringReader stringReader, ILogger<EEmemMemory> logger)
    : IEEmemMemory
{
    private readonly ILogger<EEmemMemory> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private GameClient? _gameClient;

    public nint BaseAddress { get; private set; }

    public ISafeMemoryReader MemoryReader { get; } =
        memoryReader ?? throw new ArgumentNullException(nameof(memoryReader));

    public IStringReader StringReader { get; } = stringReader ?? throw new ArgumentNullException(nameof(stringReader));

    public async ValueTask<bool> InitializeAsync(GameClient gameClient, CancellationToken cancellationToken)
    {
        _gameClient = gameClient ?? throw new ArgumentNullException(nameof(gameClient));

        const int maxAttempts = 20;
        const int delayBetweenAttemptsMs = 100;

        _logger.LogInformation(
            "Attempting to initialize EEmemMemory and find base address for PCSX2 process '{ProcessName}' (PID: {ProcessId})...",
            _gameClient.Process?.ProcessName,
            _gameClient.Process?.Id
        );

        for (int i = 0; i < maxAttempts; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            BaseAddress = GetEEmemBaseAddress(_gameClient);
            if (BaseAddress != nint.Zero)
            {
                _logger.LogInformation(
                    "EEmemory base address found at 0x{BaseAddress:X}. Initialization successful after {Attempts} attempts.",
                    BaseAddress,
                    i + 1
                );
                return true;
            }

            _logger.LogDebug(
                "EEmemory base address not found yet. Attempt {Attempt}/{MaxAttempts}. Retrying in {Delay}ms...",
                i + 1,
                maxAttempts,
                delayBetweenAttemptsMs
            );
            await Task.Delay(delayBetweenAttemptsMs, cancellationToken).ConfigureAwait(false);
        }

        _logger.LogError(
            "Failed to find EEmemory base address after {MaxAttempts} attempts. Initialization failed.",
            maxAttempts
        );
        return false;
    }

    private nint GetEEmemBaseAddress(GameClient gameClient)
    {
        if (OperatingSystem.IsWindows())
            return GetEEmemBaseAddressWindows(gameClient);

        if (OperatingSystem.IsLinux())
            return GetEEmemBaseAddressLinux(gameClient);

        throw new PlatformNotSupportedException("EEmem resolution is only supported on Windows and Linux.");
    }

    [SupportedOSPlatform("windows")]
    private nint GetEEmemBaseAddressWindows(GameClient gameClient)
    {
        nint baseAddress = gameClient.MainModuleBase;
        _logger.LogDebug("Scanning for EEmem base address. GameClient MainModuleBase: 0x{BaseAddress:X}", baseAddress);

        if (baseAddress == nint.Zero)
        {
            _logger.LogWarning("GameClient MainModuleBase is zero. Cannot proceed with EEmem search.");
            return nint.Zero;
        }

        // Read DOS Header
        try
        {
            Pe32.ImageDosHeader dosHeader = MemoryReader.ReadStruct<Pe32.ImageDosHeader>(
                gameClient.Handle,
                baseAddress
            );
            if (dosHeader.Magic != 0x5A4D) // "MZ"
            {
                _logger.LogWarning(
                    "Invalid DOS Header Magic (Expected 'MZ', Got 0x{Magic:X}). EEmem search failed.",
                    dosHeader.Magic
                );
                return nint.Zero;
            }

            // Read NT Headers
            nint ntHeadersAddr = baseAddress + dosHeader.LfaNew;
            Pe32.ImageNtHeaders64 ntHeaders = MemoryReader.ReadStruct<Pe32.ImageNtHeaders64>(
                gameClient.Handle,
                ntHeadersAddr
            );
            if (ntHeaders.Signature != 0x00004550) // "PE\0\0"
            {
                _logger.LogWarning(
                    "Invalid NT Header Signature (Expected 'PE', Got 0x{Signature:X}). EEmem search failed.",
                    ntHeaders.Signature
                );
                return nint.Zero;
            }

            // Get Export Directory
            Pe32.ImageDataDirectory exportDir = ntHeaders.OptionalHeader.DataDirectory[0];
            if (exportDir.VirtualAddress == 0)
            {
                _logger.LogDebug("Export directory not found or is empty.");
                return nint.Zero;
            }

            nint exportDirPtr = baseAddress + (int)exportDir.VirtualAddress;
            Pe32.ImageExportDirectory exportDirStruct = MemoryReader.Read<Pe32.ImageExportDirectory>(
                gameClient.Handle,
                exportDirPtr
            );

            // Read Export Names
            nint namesAddr = baseAddress + (int)exportDirStruct.AddressOfNames;
            for (int i = 0; i < exportDirStruct.NumberOfNames; i++)
            {
                nint nameRvaPtr = namesAddr + (i * 4);
                uint nameRva = MemoryReader.Read<uint>(gameClient.Handle, nameRvaPtr);
                nint namePtr = baseAddress + (int)nameRva;
                string name = StringReader.Read(gameClient.Handle, namePtr);

                _logger.LogTrace("Found export name: '{ExportName}' at RVA 0x{NameRva:X}", name, nameRva);

                if (name.Equals("EEmem", StringComparison.Ordinal))
                {
                    // Get corresponding ordinal and function address
                    nint ordinalsAddr = baseAddress + (int)exportDirStruct.AddressOfNameOrdinals;
                    ushort ordinal = MemoryReader.Read<ushort>(gameClient.Handle, ordinalsAddr + (i * 2));

                    nint functionsAddr = baseAddress + (int)exportDirStruct.AddressOfFunctions;
                    nint functionRvaPtr = functionsAddr + (ordinal * 4);
                    uint functionRva = MemoryReader.Read<uint>(gameClient.Handle, functionRvaPtr);

                    // Read the EEMem pointer (64-bit)
                    nint eememAddr = baseAddress + (int)functionRva;
                    long eememValue = MemoryReader.Read<long>(gameClient.Handle, eememAddr); // Assuming 64-bit pointer
                    _logger.LogInformation(
                        "Found 'EEmem' export! Resolved EEmemory address: 0x{EEmemValue:X}",
                        (nint)eememValue
                    );
                    return (nint)eememValue;
                }
            }
            _logger.LogDebug("Did not find 'EEmem' export among {NumberOfNames} names.", exportDirStruct.NumberOfNames);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "An error occurred while trying to get EEmem base address. This might indicate an issue with reading process memory or PE structure parsing."
            );
        }

        return nint.Zero;
    }

    /// <summary>
    /// Resolves the EEmem base address on Linux by parsing the PCSX2 ELF binary's
    /// <c>.dynsym</c> section from disk and combining the symbol offset with the
    /// runtime load base read from <c>/proc/&lt;pid&gt;/maps</c>.
    /// </summary>
    [SupportedOSPlatform("linux")]
    private nint GetEEmemBaseAddressLinux(GameClient gameClient)
    {
        int pid = (int)gameClient.Handle;
        _logger.LogDebug("Scanning for EEmem base address on Linux (PID {Pid}).", pid);

        try
        {
            // /proc/<pid>/exe is a symlink to the actual executable path.
            // It is readable by the owning user for any child process without elevated privileges,
            // unlike Process.MainModule which requires ptrace access on some .NET runtimes.
            string? exePath = File.ResolveLinkTarget($"/proc/{pid}/exe", returnFinalTarget: true)?.FullName;
            if (string.IsNullOrEmpty(exePath))
            {
                _logger.LogWarning("Cannot determine PCSX2 executable path from /proc/{Pid}/exe.", pid);
                return nint.Zero;
            }

            _logger.LogDebug("PCSX2 executable path: {ExePath}", exePath);

            nint symbolOffset = FindElfDynSymOffset(exePath, "EEmem");
            if (symbolOffset == nint.Zero)
            {
                _logger.LogWarning("'EEmem' symbol not found in ELF .dynsym of '{ExePath}'.", exePath);
                return nint.Zero;
            }

            _logger.LogDebug("'EEmem' st_value from ELF: 0x{SymbolOffset:X}", symbolOffset);

            nint loadBase = GetProcMapsLoadBase(pid, exePath);
            if (loadBase == nint.Zero)
            {
                _logger.LogWarning("Failed to determine runtime load base for PID {Pid} from /proc/maps.", pid);
                return nint.Zero;
            }

            _logger.LogDebug("Runtime load base for PCSX2: 0x{LoadBase:X}", loadBase);

            // For PIE (ET_DYN) binaries — the common case on modern Linux — the symbol value is
            // relative to the load base.  For non-PIE (ET_EXEC) the value is already absolute.
            bool isPie = IsElfPie(exePath);
            nint eememPtr = isPie ? loadBase + symbolOffset : symbolOffset;

            // The EEmem export holds a pointer to the PS2 RAM base; dereference it.
            long eememValue = MemoryReader.Read<long>(gameClient.Handle, eememPtr);
            _logger.LogInformation(
                "Found 'EEmem' export on Linux! EEmemory address: 0x{EEmemValue:X}",
                (nint)eememValue
            );
            return (nint)eememValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while resolving EEmem base address on Linux.");
            return nint.Zero;
        }
    }

    // ── ELF helpers (Linux only) ──────────────────────────────────────────────

    private const ushort EtExec = 2; // non-PIE executable
    private const ushort EtDyn = 3; // shared object / PIE executable
    private const uint ShtDynsym = 11; // SHT_DYNSYM section type

    [SupportedOSPlatform("linux")]
    private static bool IsElfPie(string exePath)
    {
        using FileStream fs = new(exePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using BinaryReader reader = new(fs);
        Span<byte> ident = stackalloc byte[16];
        if (fs.Read(ident) < 16 || ident[0] != 0x7f || ident[1] != 'E' || ident[2] != 'L' || ident[3] != 'F')
            return false;
        ushort eType = reader.ReadUInt16(); // e_type
        return eType == EtDyn;
    }

    /// <summary>
    /// Returns the <c>st_value</c> (file-relative offset for PIE, absolute VA for EXEC)
    /// of the named symbol from the binary's on-disk dynamic symbol table.
    /// Returns <see cref="nint.Zero"/> if the symbol is not found or the file cannot be parsed.
    /// Tries section headers first; falls back to PT_DYNAMIC for stripped binaries
    /// (e.g. some distribution-packaged or AppImage PCSX2 builds).
    /// </summary>
    [SupportedOSPlatform("linux")]
    private static nint FindElfDynSymOffset(string exePath, string symbolName)
    {
        using FileStream fs = new(exePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using BinaryReader reader = new(fs);

        // --- ELF identity (16 bytes) ---
        byte[] ident = reader.ReadBytes(16);
        if (ident.Length < 16 || ident[0] != 0x7f || ident[1] != 'E' || ident[2] != 'L' || ident[3] != 'F')
            return nint.Zero;

        bool is64 = ident[4] == 2; // EI_CLASS: 1 = 32-bit, 2 = 64-bit
        if (!is64)
            return nint.Zero; // Only 64-bit supported (PCSX2 ships as x86-64)

        // --- ELF64 header fields after ident ---
        reader.ReadUInt16(); // e_type
        reader.ReadUInt16(); // e_machine
        reader.ReadUInt32(); // e_version
        reader.ReadUInt64(); // e_entry
        ulong phOffset = reader.ReadUInt64(); // e_phoff
        ulong sectionHeaderOffset = reader.ReadUInt64(); // e_shoff
        reader.ReadUInt32(); // e_flags
        reader.ReadUInt16(); // e_ehsize
        reader.ReadUInt16(); // e_phentsize (56 for ELF64)
        ushort phCount = reader.ReadUInt16(); // e_phnum
        reader.ReadUInt16(); // e_shentsize  (64 for ELF64)
        ushort sectionCount = reader.ReadUInt16(); // e_shnum
        reader.ReadUInt16(); // e_shstrndx

        // --- Fast path: section headers (present unless aggressively stripped) ---
        if (sectionCount > 0 && sectionHeaderOffset != 0)
        {
            nint result = FindDynsymViaShdr(fs, reader, sectionHeaderOffset, sectionCount, symbolName);
            if (result != nint.Zero)
                return result;
        }

        // --- Fallback: PT_DYNAMIC program headers (works even when section headers stripped) ---
        if (phCount > 0 && phOffset != 0)
            return FindDynsymViaPhdrs(fs, reader, phOffset, phCount, symbolName);

        return nint.Zero;
    }

    /// <summary>
    /// Locates <paramref name="symbolName"/> in <c>.dynsym</c> via ELF64 section headers.
    /// </summary>
    [SupportedOSPlatform("linux")]
    private static nint FindDynsymViaShdr(
        FileStream fs,
        BinaryReader reader,
        ulong sectionHeaderOffset,
        ushort sectionCount,
        string symbolName
    )
    {
        // --- Read all section headers (64 bytes each for ELF64) ---
        (uint shType, ulong shOffset, ulong shSize, uint shLink)[] sections = new (uint, ulong, ulong, uint)[
            sectionCount
        ];

        fs.Seek((long)sectionHeaderOffset, SeekOrigin.Begin);
        for (int i = 0; i < sectionCount; i++)
        {
            reader.ReadUInt32(); // sh_name
            uint shType = reader.ReadUInt32(); // sh_type
            reader.ReadUInt64(); // sh_flags
            reader.ReadUInt64(); // sh_addr
            ulong shOffset = reader.ReadUInt64(); // sh_offset
            ulong shSize = reader.ReadUInt64(); // sh_size
            uint shLink = reader.ReadUInt32(); // sh_link → for DYNSYM: index of DYNSTR
            reader.ReadUInt32(); // sh_info
            reader.ReadUInt64(); // sh_addralign
            reader.ReadUInt64(); // sh_entsize
            sections[i] = (shType, shOffset, shSize, shLink);
        }

        // --- Locate .dynsym and linked .dynstr ---
        int dynsymIdx = -1;
        for (int i = 0; i < sections.Length; i++)
        {
            if (sections[i].shType == ShtDynsym)
            {
                dynsymIdx = i;
                break;
            }
        }

        if (dynsymIdx < 0)
            return nint.Zero;

        (_, ulong dynsymOffset, ulong dynsymSize, uint dynstrIdx) = sections[dynsymIdx];
        if (dynstrIdx >= sectionCount)
            return nint.Zero;

        (_, ulong dynstrOffset, ulong dynstrSize, _) = sections[dynstrIdx];

        // --- Load .dynstr into memory ---
        byte[] dynstr = new byte[dynstrSize];
        fs.Seek((long)dynstrOffset, SeekOrigin.Begin);
        if (fs.Read(dynstr) != (int)dynstrSize)
            return nint.Zero;

        // --- Walk .dynsym entries (ELF64 Sym64: 24 bytes each) ---
        //   uint32 st_name; uint8 st_info; uint8 st_other; uint16 st_shndx;
        //   uint64 st_value; uint64 st_size
        const int sym64Size = 24;
        fs.Seek((long)dynsymOffset, SeekOrigin.Begin);
        long entryCount = (long)dynsymSize / sym64Size;

        for (long i = 0; i < entryCount; i++)
        {
            uint stName = reader.ReadUInt32(); // st_name
            reader.ReadByte(); // st_info
            reader.ReadByte(); // st_other
            reader.ReadUInt16(); // st_shndx
            ulong stValue = reader.ReadUInt64(); // st_value
            reader.ReadUInt64(); // st_size

            if (stName >= dynstrSize)
                continue;

            int nameEnd = (int)stName;
            while (nameEnd < dynstr.Length && dynstr[nameEnd] != 0)
                nameEnd++;

            string name = System.Text.Encoding.UTF8.GetString(dynstr, (int)stName, nameEnd - (int)stName);
            if (name.Equals(symbolName, StringComparison.Ordinal))
                return (nint)stValue;
        }

        return nint.Zero;
    }

    /// <summary>
    /// Fallback: locates <paramref name="symbolName"/> in the dynamic symbol table by
    /// walking ELF64 PT_DYNAMIC program headers. Used when section headers are stripped.
    /// </summary>
    [SupportedOSPlatform("linux")]
    private static nint FindDynsymViaPhdrs(
        FileStream fs,
        BinaryReader reader,
        ulong phOffset,
        ushort phCount,
        string symbolName
    )
    {
        // ELF64 Phdr = 56 bytes:
        // p_type(4) + p_flags(4) + p_offset(8) + p_vaddr(8) + p_paddr(8)
        // + p_filesz(8) + p_memsz(8) + p_align(8)
        const uint PtLoad = 1;
        const uint PtDynamic = 2;

        // Dynamic tags
        const long DtHash = 4; // DT_HASH  — hash table; nchain == symbol count
        const long DtStrtab = 5; // DT_STRTAB
        const long DtSymtab = 6; // DT_SYMTAB
        const long DtStrsz = 10; // DT_STRSZ
        const int Sym64Size = 24;

        var loads = new List<(ulong vaddr, ulong fileOff, ulong filesz)>();
        ulong dynFileOff = 0,
            dynFileSz = 0;

        fs.Seek((long)phOffset, SeekOrigin.Begin);
        for (int i = 0; i < phCount; i++)
        {
            uint pType = reader.ReadUInt32();
            reader.ReadUInt32(); // p_flags
            ulong pOffset = reader.ReadUInt64();
            ulong pVaddr = reader.ReadUInt64();
            reader.ReadUInt64(); // p_paddr
            ulong pFilesz = reader.ReadUInt64();
            reader.ReadUInt64(); // p_memsz
            reader.ReadUInt64(); // p_align

            if (pType == PtLoad)
                loads.Add((pVaddr, pOffset, pFilesz));
            else if (pType == PtDynamic)
            {
                dynFileOff = pOffset;
                dynFileSz = pFilesz;
            }
        }

        if (dynFileOff == 0 || dynFileSz == 0)
            return nint.Zero;

        // Convert virtual address → file offset using the PT_LOAD segments
        ulong VaToFile(ulong va)
        {
            foreach ((ulong vaddr, ulong fileOff, ulong filesz) in loads)
                if (va >= vaddr && va < vaddr + filesz)
                    return fileOff + (va - vaddr);
            return ulong.MaxValue;
        }

        // Parse PT_DYNAMIC array: each entry is Elf64_Dyn = 16 bytes (d_tag:i64 + d_val:u64)
        ulong symtabVa = 0,
            strtabVa = 0,
            strsz = 0,
            hashVa = 0;
        fs.Seek((long)dynFileOff, SeekOrigin.Begin);
        long dynEntries = (long)dynFileSz / 16;
        for (long i = 0; i < dynEntries; i++)
        {
            long tag = reader.ReadInt64();
            ulong val = reader.ReadUInt64();
            if (tag == 0)
                break; // DT_NULL
            switch (tag)
            {
                case DtSymtab:
                    symtabVa = val;
                    break;
                case DtStrtab:
                    strtabVa = val;
                    break;
                case DtStrsz:
                    strsz = val;
                    break;
                case DtHash:
                    hashVa = val;
                    break;
            }
        }

        if (symtabVa == 0 || strtabVa == 0 || strsz == 0)
            return nint.Zero;

        ulong symtabFileOff = VaToFile(symtabVa);
        ulong strtabFileOff = VaToFile(strtabVa);
        if (symtabFileOff == ulong.MaxValue || strtabFileOff == ulong.MaxValue)
            return nint.Zero;

        // Load .dynstr
        byte[] dynstr = new byte[strsz];
        fs.Seek((long)strtabFileOff, SeekOrigin.Begin);
        if (fs.Read(dynstr) != (int)strsz)
            return nint.Zero;

        // Determine symbol count from DT_HASH (nchain field at offset 4 in the hash table)
        long symCount = -1;
        if (hashVa != 0)
        {
            ulong hashFileOff = VaToFile(hashVa);
            if (hashFileOff != ulong.MaxValue)
            {
                fs.Seek((long)hashFileOff, SeekOrigin.Begin);
                reader.ReadUInt32(); // nbucket
                symCount = reader.ReadUInt32(); // nchain = total symbol count
            }
        }

        // Walk symbol table
        fs.Seek((long)symtabFileOff, SeekOrigin.Begin);
        for (long i = 0; symCount < 0 || i < symCount; i++)
        {
            // When count is unknown, stop once we'd walk past the string table (heuristic bound)
            if (symCount < 0 && symtabVa + (ulong)(i * Sym64Size) >= strtabVa)
                break;

            uint stName = reader.ReadUInt32();
            reader.ReadByte(); // st_info
            reader.ReadByte(); // st_other
            reader.ReadUInt16(); // st_shndx
            ulong stValue = reader.ReadUInt64();
            reader.ReadUInt64(); // st_size

            if (stName >= strsz)
                continue;

            int nameEnd = (int)stName;
            while (nameEnd < dynstr.Length && dynstr[nameEnd] != 0)
                nameEnd++;

            string name = System.Text.Encoding.UTF8.GetString(dynstr, (int)stName, nameEnd - (int)stName);
            if (name.Equals(symbolName, StringComparison.Ordinal))
                return (nint)stValue;
        }

        return nint.Zero;
    }

    /// <summary>
    /// Finds the runtime load base of the first executable memory mapping for
    /// <paramref name="exePath"/> by parsing <c>/proc/&lt;pid&gt;/maps</c>.
    /// Returns <see cref="nint.Zero"/> when not found.
    /// </summary>
    /// <remarks>
    /// AppImage note: <c>/proc/&lt;pid&gt;/exe</c> and <c>/proc/&lt;pid&gt;/maps</c> both
    /// reference the binary inside the SquashFS FUSE mount
    /// (<c>/tmp/.mount_PCSX2XXXX/usr/bin/pcsx2-qt</c>), but symlink resolution inside
    /// the mount can produce differing prefixes. Exact path matching is attempted first;
    /// if no match is found, we fall back to matching by filename only so that all
    /// AppImage install variants work without configuration.
    /// </remarks>
    [SupportedOSPlatform("linux")]
    private static nint GetProcMapsLoadBase(int pid, string exePath)
    {
        // Format: start-end perms offset dev inode pathname
        // Example: 7f1234000000-7f1234001000 r-xp 00000000 08:01 12345 /usr/bin/pcsx2-qt
        // We want the first entry with file offset 00000000 and the target pathname.
        string exeName = Path.GetFileName(exePath);
        nint fallback = nint.Zero;

        foreach (string line in File.ReadLines($"/proc/{pid}/maps"))
        {
            bool exactMatch = line.Contains(exePath, StringComparison.Ordinal);
            // AppImage / symlink divergence: fall back to filename-only match
            bool nameMatch = !exactMatch && line.EndsWith(exeName, StringComparison.Ordinal);

            if (!exactMatch && !nameMatch)
                continue;

            // Verify this is the base mapping (file offset == 0) so we get the load slide
            string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 6)
                continue;

            // parts[2] is the file offset field
            if (!parts[2].Equals("00000000", StringComparison.OrdinalIgnoreCase))
                continue;

            // parts[0] is "startAddr-endAddr"
            string startHex = parts[0].Split('-')[0];
            if (!long.TryParse(startHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long startAddr))
                continue;

            if (exactMatch)
                return (nint)startAddr; // best match — return immediately

            if (fallback == nint.Zero)
                fallback = (nint)startAddr; // remember first filename-only match
        }

        return fallback;
    }

    public nint GetAddressFromPtr(nint ptrOffset)
    {
        if (_gameClient is null)
            throw new InvalidOperationException("EEmemMemory not initialized with a GameClient.");

        nint result = BaseAddress + ptrOffset;
        _logger.LogTrace("Calculated address from pointer offset 0x{PtrOffset:X}: 0x{Result:X}", ptrOffset, result);
        return result;
    }

    public nint GetAddressFromPtrChain(nint ptrOffset, params ReadOnlySpan<nint> offsets)
    {
        if (_gameClient is null)
            throw new InvalidOperationException("EEmemMemory not initialized with a GameClient.");

        nint addrNew = nint.Zero;
        nint addrOld = GetAddressFromPtr(ptrOffset); // BaseAddress + ptrOffset

        foreach (nint offset in offsets)
            addrNew = addrOld + offset; // This just adds the last offset to (BaseAddress + ptrOffset)

        return addrNew;
    }
}
