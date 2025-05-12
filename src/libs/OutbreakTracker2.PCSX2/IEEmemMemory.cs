using OutbreakTracker2.Memory;

namespace OutbreakTracker2.PCSX2;

public interface IEEmemMemory
{
    public nint BaseAddress { get; }

    public IMemoryReader MemoryReader { get; }

    public ValueTask<bool> InitializeAsync(GameClient gameClient, CancellationToken cancellationToken);

    public nint GetAddressFromPtr(nint ptrOffset);

    public nint GetAddressFromPtrChain(nint ptrOffset, params ReadOnlySpan<nint> offsets);
}