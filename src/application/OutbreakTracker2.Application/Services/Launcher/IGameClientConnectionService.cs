using OutbreakTracker2.PCSX2.Client;

namespace OutbreakTracker2.Application.Services.Launcher;

public interface IGameClientConnectionService
{
    Task<IGameClient> LaunchAndInitializeAsync(
        string fileName,
        string? arguments,
        CancellationToken cancellationToken = default
    );

    Task<IGameClient> AttachAndInitializeAsync(int processId, CancellationToken cancellationToken = default);
}
