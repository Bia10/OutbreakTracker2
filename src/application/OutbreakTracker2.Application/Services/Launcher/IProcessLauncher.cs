using System.Diagnostics;
using OutbreakTracker2.PCSX2.Client;
using R3;

namespace OutbreakTracker2.Application.Services.Launcher;

public interface IProcessLauncher
{
    Observable<ProcessModel> ProcessUpdate { get; }

    Observable<bool> IsCancelling { get; }

    Process? ClientMonitoredProcess { get; }

    IGameClient? AttachedGameClient { get; }

    Task<IGameClient> LaunchAsync(string fileName, string? arguments, CancellationToken cancellationToken = default);

    Task<IGameClient> AttachAsync(int processId);

    Task TerminateAsync(int? processId = null);

    Task KillAsync(int processId);

    Observable<string> GetErrorObservable();

    bool HasExited(int processId);

    int GetExitCode(int processId);

    IGameClient? GetActiveGameClient();
}
