using OutbreakTracker2.Memory.SafeMemory;
using OutbreakTracker2.Memory.String;

namespace OutbreakTracker2.PCSX2.EEmem;

public interface IEEmemAddressReader
{
    public ISafeMemoryReader MemoryReader { get; }

    public IStringReader StringReader { get; }

    public nint GetAddressFromPtr(nint ptrOffset);

    public nint GetAddressFromPtrChain(nint ptrOffset, params ReadOnlySpan<nint> offsets);
}
