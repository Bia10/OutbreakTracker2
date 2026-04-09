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

public sealed partial class DashboardViewModel : PageBase, IDisposable
{
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
            processLauncher
                .ProcessUpdate.ObserveOnCurrentSynchronizationContext()
                .Subscribe(processModel =>
                {
                    if (processModel.IsRunning)
                        CurrentView = ClientOverviewViewModel;
                })
        );

        _disposables.Add(
            processLocator
                .IsProcessRunningPolling("pcsx2-qt")
                .ObserveOnCurrentSynchronizationContext()
                .Subscribe(
                    onNext: isRunning =>
                    {
                        if (isRunning)
                        {
                            int? clientProcessId = null;
                            try
                            {
                                clientProcessId = processLauncher.ClientMonitoredProcess?.Id;
                            }
                            catch (InvalidOperationException)
                            {
                                // Process handle invalidated between null check and Id access (race with exit)
                            }

                            IReadOnlyList<int> processIds = processLocator.GetProcessIds("pcsx2-qt");

                            if (clientProcessId.HasValue && processIds.Contains(clientProcessId.Value))
                                HandleMonitoredProcess();
                            else
                                HandleUnmonitoredProcess(processIds);
                        }
                        else
                        {
                            HandleNoRunningProcess();
                        }
                    },
                    onErrorResume: ex => _logger.LogError(ex, "Error in process polling"),
                    onCompleted: _ => _logger.LogInformation("Process polling completed")
                )
        );
    }

    public void Dispose() => _disposables.Dispose();

    private void HandleMonitoredProcess()
    {
        _logger.LogInformation("Monitored process is running");
        IsClientRunning = true;
        CurrentView = ClientOverviewViewModel;
    }

    private void HandleUnmonitoredProcess(IReadOnlyList<int> processIds)
    {
        _logger.LogInformation("Unmonitored process is running");
        IsClientRunning = true;
        ClientAlreadyRunningViewModel!.UpdateProcesses(processIds);
        CurrentView = ClientAlreadyRunningViewModel;
    }

    private void HandleNoRunningProcess()
    {
        _logger.LogInformation("No running process found");
        IsClientRunning = false;
        CurrentView = ClientNotRunningViewModel;
    }
}
