using OutbreakTracker2.Memory;
using OutbreakTracker2.WinInterop.Structs;

namespace OutbreakTracker2.PCSX2;

public sealed class EEmemMemory : IEEmemMemory
{
    private readonly GameClient _gameClient;

    public nint BaseAddress { get; private set; }

    public IMemoryReader MemoryReader { get; }

    public EEmemMemory(GameClient gameClient, IMemoryReader memoryReader)
    {
        _gameClient = gameClient ?? throw new ArgumentNullException(nameof(gameClient));
        MemoryReader = memoryReader ?? throw new ArgumentNullException(nameof(memoryReader));
    }

    public async ValueTask<bool> InitializeAsync(GameClient gameClient, CancellationToken cancellationToken)
    {
        const int MaxAttempts = 20;
        const int DelayBetweenAttemptsMs = 100;

        for (int i = 0; i < MaxAttempts; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            BaseAddress = GetEEmemBaseAddress(gameClient);
            if (BaseAddress != nint.Zero)
                return true;

            await Task.Delay(DelayBetweenAttemptsMs, cancellationToken);
        }

        return false;
    }

    private nint GetEEmemBaseAddress(GameClient gameClient)
    {
        nint baseAddress = gameClient.MainModuleBase;

        // Read DOS Header
        PE32.ImageDosHeader dosHeader = MemoryReader.Read<PE32.ImageDosHeader>(gameClient.Handle, baseAddress);
        if (dosHeader.Magic != 0x5A4D) // "MZ"
            return nint.Zero;

        // Read NT Headers
        nint ntHeadersAddr = baseAddress + dosHeader.LfaNew;
        PE32.ImageNtHeaders64 ntHeaders = MemoryReader.Read<PE32.ImageNtHeaders64>(gameClient.Handle, ntHeadersAddr);
        if (ntHeaders.Signature != 0x00004550) // "PE\0\0"
            return nint.Zero;

        // Get Export Directory
        PE32.ImageDataDirectory exportDir = ntHeaders.OptionalHeader.DataDirectory[0];
        if (exportDir.VirtualAddress is 0)
            return nint.Zero;

        nint exportDirPtr = baseAddress + (int)exportDir.VirtualAddress;
        PE32.ImageExportDirectory exportDirStruct = MemoryReader.Read<PE32.ImageExportDirectory>(gameClient.Handle, exportDirPtr);

        // Read Export Names
        nint namesAddr = baseAddress + (int)exportDirStruct.AddressOfNames;
        for (int i = 0; i < exportDirStruct.NumberOfNames; i++)
        {
            nint nameRvaPtr = namesAddr + (i * 4);
            uint nameRva = MemoryReader.Read<uint>(gameClient.Handle, nameRvaPtr);
            nint namePtr = baseAddress + (int)nameRva;
            string name = MemoryReader.ReadString(gameClient.Handle, namePtr);

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
                return (nint)eememValue;
            }
        }

        return nint.Zero;
    }

    public nint GetAddressFromPtr(nint ptrOffset)
    {
        nint result = BaseAddress + ptrOffset;
        return result;
    }

    // TODO: rebasing multiple offsets this will currently work only for 1 offset added on top of BaseAddress + ptrOffset;
    public nint GetAddressFromPtrChain(nint ptrOffset, params ReadOnlySpan<nint> offsets)
    {
        nint addrNew = nint.Zero;
        nint addrOld = GetAddressFromPtr(ptrOffset);

        foreach (nint offset in offsets)
            addrNew = addrOld + offset;

        return addrNew;
    }
}
