using System.Diagnostics;

namespace OutbreakTracker2.PCSX2.Client;

public sealed class GameClientFactory : IGameClientFactory
{
    public async Task<GameClient> CreateAndAttachGameClientAsync(Process process, CancellationToken cancellationToken)
    {
        GameClient gameClient = new();
        try
        {
            await Task.Run(() => gameClient.Attach(process), cancellationToken).ConfigureAwait(false);
            return gameClient;
        }
        catch (Exception ex)
        {
            gameClient.Dispose();
            throw new InvalidOperationException($"Failed to attach GameClient to process {process.Id}", ex);
        }
    }
}