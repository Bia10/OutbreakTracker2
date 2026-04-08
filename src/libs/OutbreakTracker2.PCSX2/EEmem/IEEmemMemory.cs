using OutbreakTracker2.PCSX2.Client;

namespace OutbreakTracker2.PCSX2.EEmem;

public interface IEEmemMemory : IEEmemAddressReader
{
    public ValueTask<bool> InitializeAsync(IGameClient gameClient, CancellationToken cancellationToken);

    public nint BaseAddress { get; }
}
