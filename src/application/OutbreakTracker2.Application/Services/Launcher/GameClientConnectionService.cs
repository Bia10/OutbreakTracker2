using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.PCSX2.Client;

namespace OutbreakTracker2.Application.Services.Launcher;

public sealed class GameClientConnectionService(
    ILogger<GameClientConnectionService> logger,
    IProcessLauncher processLauncher,
    IDataManager dataManager
) : IGameClientConnectionService
{
    private readonly ILogger<GameClientConnectionService> _logger = logger;
    private readonly IProcessLauncher _processLauncher = processLauncher;
    private readonly IDataManager _dataManager = dataManager;

    public async Task<IGameClient> LaunchAndInitializeAsync(
        string fileName,
        string? arguments,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation("Launching client process and initializing data manager");

        IGameClient gameClient = await _processLauncher
            .LaunchAsync(fileName, arguments, cancellationToken)
            .ConfigureAwait(false);
        await _dataManager.InitializeAsync(gameClient, cancellationToken).ConfigureAwait(false);

        return gameClient;
    }

    public async Task<IGameClient> AttachAndInitializeAsync(
        int processId,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation("Attaching to process {ProcessId} and initializing data manager", processId);

        IGameClient gameClient = await _processLauncher.AttachAsync(processId).ConfigureAwait(false);
        await _dataManager.InitializeAsync(gameClient, cancellationToken).ConfigureAwait(false);

        return gameClient;
    }
}
