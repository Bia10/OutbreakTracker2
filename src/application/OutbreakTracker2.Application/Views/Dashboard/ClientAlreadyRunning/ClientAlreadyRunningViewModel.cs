using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Launcher;
using OutbreakTracker2.Application.Services.Toasts;
using OutbreakTracker2.PCSX2.Client;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientAlreadyRunning;

public partial class ClientAlreadyRunningViewModel(
    IProcessLauncher processLauncher,
    IToastService toastService,
    ILogger<ClientAlreadyRunningViewModel> logger,
    IDataManager dataManager
) : ObservableObject
{
    private readonly ILogger<ClientAlreadyRunningViewModel> _logger = logger;
    private readonly IProcessLauncher _processLauncher = processLauncher;
    private readonly IToastService _toastService = toastService;
    private readonly IDataManager _dataManager = dataManager;

    [ObservableProperty]
    private ObservableCollection<ProcessModel> _runningProcesses = [];

    public void UpdateProcesses(IReadOnlyList<int> processIds)
    {
        RunningProcesses.Clear();

        foreach (int id in processIds)
            try
            {
                Process process = Process.GetProcessById(id);
                RunningProcesses.Add(
                    new ProcessModel
                    {
                        Id = process.Id,
                        Name = process.ProcessName,
                        StartTime = GetSafeStartTime(process),
                        IsRunning = !process.HasExited,
                    }
                );
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Process {ProcessId} no longer exists", id);
            }
    }

    [RelayCommand]
    public async Task AttachToProcessAsync(int processId)
    {
        try
        {
            await _processLauncher.AttachAsync(processId).ConfigureAwait(false);
            await _toastService.InvokeSuccessToastAsync("Successfully attached to process!").ConfigureAwait(false);

            GameClient? activeGameClient = _processLauncher.GetActiveGameClient();
            if (activeGameClient is not null)
            {
                await _dataManager.InitializeAsync(activeGameClient, CancellationToken.None).ConfigureAwait(false);
                _logger.LogInformation("DataManager initialized successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to attach to process {ProcessId}", processId);
            await _toastService.InvokeErrorToastAsync($"Attachment failed: {ex.Message}").ConfigureAwait(false);
            UpdateProcesses([processId]);
        }
    }

    private DateTime GetSafeStartTime(Process process)
    {
        try
        {
            return process.StartTime;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Process {ProcessId} has no start time", process.Id);
            return DateTime.MinValue;
        }
    }
}
