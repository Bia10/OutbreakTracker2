using OutbreakTracker2.PCSX2.Client;
using R3;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace OutbreakTracker2.App.Services.ProcessLauncher;

public interface IProcessLauncher
{
    Observable<ProcessModel> ProcessUpdate { get; }

    Observable<bool> IsCancelling { get; }

    Process? ClientMonitoredProcess { get; }

    GameClient? AttachedGameClient { get; }

    Task<GameClient> LaunchAsync(string fileName, string? arguments, CancellationToken cancellationToken = default);

    Task<GameClient> AttachAsync(int processId);

    Task TerminateAsync(int? processId = null);

    Observable<string> GetErrorObservable();

    bool HasExited(int processId);

    int GetExitCode(int processId);

    GameClient? GetActiveGameClient();
}
