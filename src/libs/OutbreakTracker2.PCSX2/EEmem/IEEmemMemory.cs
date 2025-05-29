using OutbreakTracker2.Memory.MemoryReader;
using OutbreakTracker2.Memory.StringReader;
using OutbreakTracker2.PCSX2.Client;

namespace OutbreakTracker2.PCSX2.EEmem;

public interface IEEmemMemory
{
    public ValueTask<bool> InitializeAsync(GameClient gameClient, CancellationToken cancellationToken);

    public nint BaseAddress { get; }

    public ISafeMemoryReader MemoryReader { get; }

    public IStringReader StringReader { get; }

    public nint GetAddressFromPtr(nint ptrOffset);

    public nint GetAddressFromPtrChain(nint ptrOffset, params ReadOnlySpan<nint> offsets);
}