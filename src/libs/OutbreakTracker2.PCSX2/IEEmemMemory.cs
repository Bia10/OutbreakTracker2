using OutbreakTracker2.Memory;

namespace OutbreakTracker2.PCSX2Memory;

public interface IEEmemMemory
{
    public IMemoryReader MemoryReader { get; }

    public nint BaseAddress { get; }

    public nint GetEEmemBaseAddress();

    public nint GetAddressFromPtr(nint ptrOffset);

    public nint GetAddressFromPtrChain(nint ptrOffset, params nint[] offsets);
}