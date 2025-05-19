using OutbreakTracker2.Memory.MemoryReader;
using OutbreakTracker2.Memory.StringReader;

namespace OutbreakTracker2.PCSX2;

public interface IEEmemMemory
{
    public nint BaseAddress { get; }

    public ISafeMemoryReader MemoryReader { get; }

    public IStringReader StringReader { get; }

    public nint GetAddressFromPtr(nint ptrOffset);

    public nint GetAddressFromPtrChain(nint ptrOffset, params ReadOnlySpan<nint> offsets);
}