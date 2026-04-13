using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using OutbreakTracker2.Application.Services.Launcher;
using OutbreakTracker2.Application.Services.Toasts;
using OutbreakTracker2.Application.Utilities;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientAlreadyRunning;

public sealed partial class ClientAlreadyRunningViewModel(
    IProcessLauncher processLauncher,
    IGameClientConnectionService gameClientConnectionService,
    IToastService toastService,
    ILogger<ClientAlreadyRunningViewModel> logger
) : ObservableObject
{
    private readonly ILogger<ClientAlreadyRunningViewModel> _logger = logger;
    private readonly IProcessLauncher _processLauncher = processLauncher;
    private readonly IGameClientConnectionService _gameClientConnectionService = gameClientConnectionService;
    private readonly IToastService _toastService = toastService;

    [ObservableProperty]
    private ObservableList<ProcessModel> _runningProcesses = [];

    public void UpdateProcesses(IReadOnlyList<int> processIds)
    {
        RunningProcesses.Clear();
        List<ProcessModel> discoveredProcesses = [];

        foreach (int id in processIds)
            try
            {
                Process process = Process.GetProcessById(id);
                discoveredProcesses.Add(
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

        RunningProcesses.AddRange(discoveredProcesses);
    }

    [RelayCommand]
    public async Task AttachToProcessAsync(int processId)
    {
        try
        {
            await _gameClientConnectionService
                .AttachAndInitializeAsync(processId, CancellationToken.None)
                .ConfigureAwait(false);
            await _toastService.InvokeSuccessToastAsync("Successfully attached to process!").ConfigureAwait(false);
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
            await _processLauncher.KillAsync(processId).ConfigureAwait(false);
            ProcessModel? model = RunningProcesses.FirstOrDefault(p => p.Id == processId);
            if (model is not null)
                RunningProcesses.Remove(model);
            await _toastService.InvokeSuccessToastAsync("Process terminated.").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to terminate process {ProcessId}", processId);
            await _toastService.InvokeErrorToastAsync($"Termination failed: {ex.Message}").ConfigureAwait(false);
        }
    }
}
