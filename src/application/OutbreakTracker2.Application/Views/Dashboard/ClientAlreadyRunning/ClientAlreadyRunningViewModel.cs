using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Launcher;
using OutbreakTracker2.Application.Services.Toasts;
using OutbreakTracker2.Application.Utilities;
using OutbreakTracker2.PCSX2.Client;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientAlreadyRunning;

public sealed partial class ClientAlreadyRunningViewModel(
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
                        StartTime = process.GetSafeStartTime(),
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

            IGameClient? activeGameClient = _processLauncher.GetActiveGameClient();
            if (activeGameClient is not null)
            {
                await _dataManager.InitializeAsync(activeGameClient, CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to attach to process {ProcessId}", processId);
            await _toastService.InvokeErrorToastAsync($"Attachment failed: {ex.Message}").ConfigureAwait(false);
            UpdateProcesses([processId]);
        }
    }

    [RelayCommand]
    public async Task TerminateProcessAsync(int processId)
    {
        try
        {
            Process process = Process.GetProcessById(processId);
            process.Kill();
            RunningProcesses.Remove(RunningProcesses.First(p => p.Id == processId));
            await _toastService.InvokeSuccessToastAsync("Process terminated.").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to terminate process {ProcessId}", processId);
            await _toastService.InvokeErrorToastAsync($"Termination failed: {ex.Message}").ConfigureAwait(false);
        }
    }
}
