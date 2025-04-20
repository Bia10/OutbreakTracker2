using CommunityToolkit.Mvvm.ComponentModel;
using Material.Icons;
using OutbreakTracker2.App.Pages;
using OutbreakTracker2.App.Services.ProcessLauncher;
using OutbreakTracker2.App.Services.ProcessLocator;
using OutbreakTracker2.App.Views.Dashboard.ClientNotRunning;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview;
using R3;
using System;
using System.Collections.Generic;

namespace OutbreakTracker2.App.Views.Dashboard;

public partial class DashboardViewModel : PageBase
{
    [ObservableProperty]
    private bool _isServerRunning;

    [ObservableProperty]
    private object? _currentView;

    public ClientNotRunningViewModel? ClientNotRunningViewModel { get; }

    public ClientOverviewViewModel? ClientOverviewViewModel { get; }

    public DashboardViewModel(
        IProcessLocator processLocator,
        IProcessLauncher processLauncher,
        ClientNotRunningViewModel clientNotRunningViewModel,
        ClientOverviewViewModel clientOverviewViewModel)
        : base("Dashboard", MaterialIconKind.MonitorDashboard, int.MinValue)
    {
        ClientNotRunningViewModel = clientNotRunningViewModel;
        ClientOverviewViewModel = clientOverviewViewModel;

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
            onErrorResume: ex => Console.WriteLine("Resuming with error:{0}", ex),
            onCompleted: result => Console.WriteLine("Completed with result: {0}", result));
    }

    private void HandleMonitoredProcess()
    {
        IsServerRunning = true;
        CurrentView = ClientOverviewViewModel;
    }

    private void HandleUnmonitoredProcess(List<int> processIds)
    {
        // TODO: attach to unmonitored process
        throw new NotImplementedException("HandleUnmonitoredProcess is not implemented.");
    }

    private void HandleNoRunningProcess()
    {
        IsServerRunning = false;
        CurrentView = ClientNotRunningViewModel;
    }
}
