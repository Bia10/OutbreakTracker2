using System.Diagnostics;

namespace OutbreakTracker2.PCSX2.Client;

public interface IGameClientFactory
{
    public Task<GameClient> CreateAndAttachGameClientAsync(Process process, CancellationToken cancellationToken);
}