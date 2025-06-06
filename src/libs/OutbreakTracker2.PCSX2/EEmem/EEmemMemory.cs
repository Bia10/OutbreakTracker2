using Microsoft.Extensions.Logging;
using OutbreakTracker2.Memory.SafeMemory;
using OutbreakTracker2.Memory.String;
using OutbreakTracker2.PCSX2.Client;
using OutbreakTracker2.WinInterop.Structs;

namespace OutbreakTracker2.PCSX2.EEmem;

public sealed class EEmemMemory : IEEmemMemory
{
    private readonly ILogger<EEmemMemory> _logger;
    private GameClient? _gameClient;

    public nint BaseAddress { get; private set; }

    public ISafeMemoryReader MemoryReader { get; }

    public IStringReader StringReader { get; }

    public EEmemMemory(ISafeMemoryReader memoryReader, IStringReader stringReader, ILogger<EEmemMemory> logger)
    {
        MemoryReader = memoryReader ?? throw new ArgumentNullException(nameof(memoryReader));
        StringReader = stringReader ?? throw new ArgumentNullException(nameof(stringReader));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<bool> InitializeAsync(GameClient gameClient, CancellationToken cancellationToken)
    {
        _gameClient = gameClient ?? throw new ArgumentNullException(nameof(gameClient));

        const int maxAttempts = 20;
        const int delayBetweenAttemptsMs = 100;

        _logger.LogInformation("Attempting to initialize EEmemMemory and find base address for PCSX2 process '{ProcessName}' (PID: {ProcessId})...",
            _gameClient.Process?.ProcessName, _gameClient.Process?.Id);

        for (int i = 0; i < maxAttempts; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            BaseAddress = GetEEmemBaseAddress(_gameClient);
            if (BaseAddress != nint.Zero)
            {
                _logger.LogInformation("EEmemory base address found at 0x{BaseAddress:X}. Initialization successful after {Attempts} attempts.", BaseAddress, i + 1);
                return true;
            }

            _logger.LogDebug("EEmemory base address not found yet. Attempt {Attempt}/{MaxAttempts}. Retrying in {Delay}ms...", i + 1, maxAttempts, delayBetweenAttemptsMs);
            await Task.Delay(delayBetweenAttemptsMs, cancellationToken)
                .ConfigureAwait(false);
        }

        _logger.LogError("Failed to find EEmemory base address after {MaxAttempts} attempts. Initialization failed.", maxAttempts);
        return false;
    }

    private nint GetEEmemBaseAddress(GameClient gameClient)
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
            Pe32.ImageDosHeader dosHeader = MemoryReader.ReadStruct<Pe32.ImageDosHeader>(gameClient.Handle, baseAddress);
            if (dosHeader.Magic != 0x5A4D) // "MZ"
            {
                _logger.LogWarning("Invalid DOS Header Magic (Expected 'MZ', Got 0x{Magic:X}). EEmem search failed.", dosHeader.Magic);
                return nint.Zero;
            }

            // Read NT Headers
            nint ntHeadersAddr = baseAddress + dosHeader.LfaNew;
            Pe32.ImageNtHeaders64 ntHeaders = MemoryReader.ReadStruct<Pe32.ImageNtHeaders64>(gameClient.Handle, ntHeadersAddr);
            if (ntHeaders.Signature != 0x00004550) // "PE\0\0"
            {
                _logger.LogWarning("Invalid NT Header Signature (Expected 'PE', Got 0x{Signature:X}). EEmem search failed.", ntHeaders.Signature);
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
            Pe32.ImageExportDirectory exportDirStruct = MemoryReader.Read<Pe32.ImageExportDirectory>(gameClient.Handle, exportDirPtr);

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
                    _logger.LogInformation("Found 'EEmem' export! Resolved EEmemory address: 0x{EEmemValue:X}", (nint)eememValue);
                    return (nint)eememValue;
                }
            }
            _logger.LogDebug("Did not find 'EEmem' export among {NumberOfNames} names.", exportDirStruct.NumberOfNames);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while trying to get EEmem base address. This might indicate an issue with reading process memory or PE structure parsing.");
        }

        return nint.Zero;
    }

    public nint GetAddressFromPtr(nint ptrOffset)
    {
        if (_gameClient is null) throw new InvalidOperationException("EEmemMemory not initialized with a GameClient.");

        nint result = BaseAddress + ptrOffset;
        _logger.LogTrace("Calculated address from pointer offset 0x{PtrOffset:X}: 0x{Result:X}", ptrOffset, result);
        return result;
    }

    public nint GetAddressFromPtrChain(nint ptrOffset, params ReadOnlySpan<nint> offsets)
    {
        if (_gameClient is null) throw new InvalidOperationException("EEmemMemory not initialized with a GameClient.");

        nint addrNew = nint.Zero;
        nint addrOld = GetAddressFromPtr(ptrOffset); // BaseAddress + ptrOffset

        foreach (nint offset in offsets)
            addrNew = addrOld + offset; // This just adds the last offset to (BaseAddress + ptrOffset)

        return addrNew;
    }
}