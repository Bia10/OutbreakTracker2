using CommunityToolkit.Mvvm.ComponentModel;
using Material.Icons;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Pages;
using OutbreakTracker2.Application.Services.Launcher;
using OutbreakTracker2.Application.Services.Locator;
using OutbreakTracker2.Application.Views.Dashboard.ClientAlreadyRunning;
using OutbreakTracker2.Application.Views.Dashboard.ClientNotRunning;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview;
using R3;

namespace OutbreakTracker2.Application.Views.Dashboard;

public sealed partial class DashboardViewModel : PageBase, IDisposable, IAsyncDisposable
{
    private enum ProcessViewState
    {
        None,
        Monitored,
        Unmonitored,
    }

    private readonly record struct ProcessStateSnapshot(ProcessViewState State, IReadOnlyList<int> ProcessIds);

    private readonly ILogger<DashboardViewModel> _logger;
    private DisposableBag _disposables;

    [ObservableProperty]
    private bool _isClientRunning;

    [ObservableProperty]
    private object? _currentView;

    public ClientNotRunningViewModel? ClientNotRunningViewModel { get; }

    public ClientOverviewViewModel? ClientOverviewViewModel { get; }

    public ClientAlreadyRunningViewModel? ClientAlreadyRunningViewModel { get; }

    public DashboardViewModel(
        IProcessLocator processLocator,
        IProcessLauncher processLauncher,
        ClientNotRunningViewModel clientNotRunningViewModel,
        ClientOverviewViewModel clientOverviewViewModel,
        ClientAlreadyRunningViewModel clientAlreadyRunningViewModel,
        ILogger<DashboardViewModel> logger
    )
        : base("Dashboard", MaterialIconKind.MonitorDashboard, int.MinValue)
    {
        ClientNotRunningViewModel = clientNotRunningViewModel;
        ClientOverviewViewModel = clientOverviewViewModel;
        ClientAlreadyRunningViewModel = clientAlreadyRunningViewModel;
        _logger = logger;

        _disposables.Add(
            Observable
                .Merge(
                    processLauncher.ProcessUpdate.Select(_ => ResolveProcessState(processLocator, processLauncher)),
                    processLocator
                        .IsProcessRunningPolling("pcsx2-qt")
                        .Select(_ => ResolveProcessState(processLocator, processLauncher))
                )
                .ObserveOnCurrentSynchronizationContext()
                .Subscribe(
                    processModel =>
                    {
                        ApplyProcessState(processModel);
                    },
                    onErrorResume: ex => _logger.LogError(ex, "Error while monitoring dashboard process state"),
                    onCompleted: _ => _logger.LogInformation("Dashboard process state monitoring completed")
                )
        );
    }

    public void Dispose() => _disposables.Dispose();

    public async ValueTask DisposeAsync()
    {
        _disposables.Dispose();
        if (ClientOverviewViewModel is not null)
            await ClientOverviewViewModel.DisposeAsync().ConfigureAwait(false);
    }

    private void ApplyProcessState(ProcessStateSnapshot processState)
    {
        switch (processState.State)
        {
            case ProcessViewState.Monitored:
                _logger.LogInformation("Monitored process is running");
                IsClientRunning = true;
                CurrentView = ClientOverviewViewModel;
                break;

            case ProcessViewState.Unmonitored:
                _logger.LogInformation("Unmonitored process is running");
                IsClientRunning = true;
                ClientAlreadyRunningViewModel!.UpdateProcesses(processState.ProcessIds);
                CurrentView = ClientAlreadyRunningViewModel;
                break;

            default:
                _logger.LogInformation("No running process found");
                IsClientRunning = false;
                CurrentView = ClientNotRunningViewModel;
                break;
        }
    }

    private static ProcessStateSnapshot ResolveProcessState(
        IProcessLocator processLocator,
        IProcessLauncher processLauncher
    )
    {
        IReadOnlyList<int> processIds = processLocator.GetProcessIds("pcsx2-qt");
        if (processIds.Count == 0)
            return new ProcessStateSnapshot(ProcessViewState.None, processIds);

        int? clientProcessId = null;
        try
        {
            clientProcessId = processLauncher.ClientMonitoredProcess?.Id;
        }
        catch (InvalidOperationException)
        {
            // Process handle invalidated between null check and Id access (race with exit)
        }

        ProcessViewState state =
            clientProcessId.HasValue && processIds.Contains(clientProcessId.Value)
                ? ProcessViewState.Monitored
                : ProcessViewState.Unmonitored;

        return new ProcessStateSnapshot(state, processIds);
    }
}
