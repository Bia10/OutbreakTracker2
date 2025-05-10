using CommunityToolkit.Mvvm.ComponentModel;
using Material.Icons;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.App.Pages;
using OutbreakTracker2.App.Services.ProcessLauncher;
using OutbreakTracker2.App.Services.ProcessLocator;
using OutbreakTracker2.App.Views.Dashboard.ClientAlreadyRunning;
using OutbreakTracker2.App.Views.Dashboard.ClientNotRunning;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview;
using R3;
using System.Collections.Generic;

namespace OutbreakTracker2.App.Views.Dashboard;

public partial class DashboardViewModel : PageBase
{
    [ObservableProperty]
    private bool _isClientRunning;

    [ObservableProperty]
    private object? _currentView;

    private readonly ILogger<DashboardViewModel> _logger;

    public ClientNotRunningViewModel? ClientNotRunningViewModel { get; }
    public ClientOverviewViewModel? ClientOverviewViewModel { get; }
    public ClientAlreadyRunningViewModel? ClientAlreadyRunningViewModel { get; }

    public DashboardViewModel(
        IProcessLocator processLocator,
        IProcessLauncher processLauncher,
        ClientNotRunningViewModel clientNotRunningViewModel,
        ClientOverviewViewModel clientOverviewViewModel,
        ClientAlreadyRunningViewModel clientAlreadyRunningViewModel,
        ILogger<DashboardViewModel> logger)
        : base("Dashboard", MaterialIconKind.MonitorDashboard, int.MinValue)
    {
        ClientNotRunningViewModel = clientNotRunningViewModel;
        ClientOverviewViewModel = clientOverviewViewModel;
        ClientAlreadyRunningViewModel = clientAlreadyRunningViewModel;
        _logger = logger;
        _logger.LogInformation("Initializing DashboardViewModel");

        processLauncher.ProcessUpdate.Subscribe(processModel =>
        {
            if (processModel.IsRunning)
                CurrentView = ClientOverviewViewModel;
        });

        processLocator.IsProcessRunningPolling("pcsx2-qt").Subscribe(
            onNext: isRunning =>
            {
                if (isRunning)
                {
                    int? clientProcessId = processLauncher.ClientMonitoredProcess?.Id;
                    List<int> processIds = processLocator.GetProcessIds("pcsx2-qt");

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
            onCompleted: _ => _logger.LogInformation("Process polling completed"));
    }

    private void HandleMonitoredProcess()
    {
        IsClientRunning = true;
        CurrentView = ClientOverviewViewModel;
    }

    private void HandleUnmonitoredProcess(List<int> processIds)
    {
        IsClientRunning = true;
        ClientAlreadyRunningViewModel!.UpdateProcesses(processIds);
        CurrentView = ClientAlreadyRunningViewModel;
    }

    private void HandleNoRunningProcess()
    {
        IsClientRunning = false;
        CurrentView = ClientNotRunningViewModel;
    }
}
