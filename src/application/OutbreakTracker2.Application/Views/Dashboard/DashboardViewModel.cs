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
using System.Collections.Generic;
using System.Linq;

namespace OutbreakTracker2.Application.Views.Dashboard;

public partial class DashboardViewModel : PageBase
{
    private readonly ILogger<DashboardViewModel> _logger;

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
            onCompleted: _ => _logger.LogInformation("Process polling completed"));
    }

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