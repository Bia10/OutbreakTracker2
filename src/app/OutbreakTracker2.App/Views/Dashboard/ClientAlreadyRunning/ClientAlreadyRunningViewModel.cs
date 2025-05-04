using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.App.Services.ProcessLauncher;
using OutbreakTracker2.App.Services.Toasts;

namespace OutbreakTracker2.App.Views.Dashboard.ClientAlreadyRunning;

public partial class ClientAlreadyRunningViewModel : ObservableObject, IDisposable
{
    private readonly IProcessLauncher _processLauncher;
    private readonly IToastService _toastService;
    private readonly ILogger<ClientAlreadyRunningViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<ProcessModel> _runningProcesses = [];

    public ClientAlreadyRunningViewModel(
        IProcessLauncher processLauncher,
        IToastService toastService,
        ILogger<ClientAlreadyRunningViewModel> logger)
    {
        _processLauncher = processLauncher;
        _toastService = toastService;
        _logger = logger;
    }

    public void UpdateProcesses(List<int> processIds)
    {
        RunningProcesses.Clear();

        foreach (int id in processIds)
            try
            {
                var process = Process.GetProcessById(id);
                RunningProcesses.Add(new ProcessModel
                {
                    Id = process.Id,
                    Name = process.ProcessName,
                    StartTime = GetSafeStartTime(process),
                    IsRunning = !process.HasExited
                });
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
            await _processLauncher.AttachAsync(processId);
            await _toastService.InvokeSuccessToastAsync("Successfully attached to process!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to attach to process {ProcessId}", processId);
            await _toastService.InvokeErrorToastAsync($"Attachment failed: {ex.Message}");
            UpdateProcesses([processId]);
        }
    }

    private static DateTime GetSafeStartTime(Process process)
    {
        try { return process.StartTime; }
        catch { return DateTime.MinValue; }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}