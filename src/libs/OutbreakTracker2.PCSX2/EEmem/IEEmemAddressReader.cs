using OutbreakTracker2.Memory.SafeMemory;
using OutbreakTracker2.Memory.String;

namespace OutbreakTracker2.PCSX2.EEmem;

public interface IEEmemAddressReader
{
    public ISafeMemoryReader MemoryReader { get; }

    public IStringReader StringReader { get; }

    public nint GetAddressFromPtr(nint ptrOffset);

    public nint GetAddressFromPtrChain(nint ptrOffset, params ReadOnlySpan<nint> offsets);

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="address"/> falls within the
    /// PS2 EEmem buffer (32 MiB window starting at <see cref="IEEmemMemory.BaseAddress"/>).
    /// Always returns <see langword="false"/> when the base address has not been resolved yet.
    /// </summary>
    public bool IsAddressInBounds(nint address);
}
