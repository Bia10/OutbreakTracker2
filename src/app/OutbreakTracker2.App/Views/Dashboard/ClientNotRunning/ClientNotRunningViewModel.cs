using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.App.Services.ProcessLauncher;
using OutbreakTracker2.App.Services.Toasts;
using R3;
using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using OutbreakTracker2.App.Services.FileLocators;
using SukiUI.Toasts;

namespace OutbreakTracker2.App.Views.Dashboard.ClientNotRunning;

public partial class ClientNotRunningViewModel : ObservableObject, IDisposable
{
    private readonly CompositeDisposable _disposables = [];
    private readonly ILogger<ClientNotRunningViewModel> _logger;
    private readonly IToastService _toastService;
    private readonly IProcessLauncher _processLauncher;
    private readonly IPCSX2Locator _pcsx2Locator;

    private const byte LaunchDelay = 3;
    private const byte LaunchTimeout = 6;

    [ObservableProperty]
    private bool _isClientLaunching;

    [ObservableProperty]
    private bool _isCancellationRequested;

    public ClientNotRunningViewModel(
        ILogger<ClientNotRunningViewModel> logger,
        IToastService toastService,
        IProcessLauncher processLauncher,
        IPCSX2Locator pcsx2Locator
    )
    {
        _logger = logger;
        _toastService = toastService;
        _processLauncher = processLauncher;
        _pcsx2Locator = pcsx2Locator;
    }

    [RelayCommand]
    public async Task LaunchFile1Async()
    {
        string? isoPath = await _pcsx2Locator.FindOutbreakFile1Async();
        if (isoPath is null)
        {
            await _toastService.InvokeErrorToastAsync(
                "Outbreak File 1 ISO not found! Please check your game installations.");
            return;
        }

        await LaunchGameAsync(isoPath);
    }

    [RelayCommand]
    public async Task LaunchFile2Async()
    {
        string? isoPath = await _pcsx2Locator.FindOutbreakFile2Async();
        if (isoPath is null)
        {
            await _toastService.InvokeErrorToastAsync(
                "Outbreak File 2 ISO not found! Please check your game installations.");
            return;
        }

        await LaunchGameAsync(isoPath);
    }

    [RelayCommand]
    public async Task LaunchGameAsync(string isoPath)
    {
        _logger.LogInformation("Launching client");
        IsClientLaunching = true;

        var cts = new CancellationTokenSource();

        _processLauncher.IsCancelling.Subscribe(
                state: this,
                onNext: (isCancelling, vm) => vm.IsCancellationRequested = isCancelling,
                onErrorResume: (ex, vm) => vm._logger.LogError(ex, "Error in IsCancelling observable, resuming pipeline"),
                onCompleted: (result, vm) => vm._logger.LogInformation("IsCancelling observable completed with {Status}", result.IsSuccess ? "success" : "failure"))
            .AddTo(_disposables);

        ISukiToast launchToast = _toastService.CreateInfoToastWithCancelButton(
            "Launch client", "Cancel client launch", toast =>
            {
                CancellationTokenSource? localCts = cts;
                if (localCts is { IsCancellationRequested: false })
                {
                    localCts.Cancel();
                    localCts.Dispose();
                    cts = null;
                    _logger.LogInformation("User cancelled client launch");
                    IsClientLaunching = false;
                }

                _toastService.DismissToastAsync(toast);
            });

        _processLauncher.ProcessUpdate.Where(p => p.IsRunning)
            .Take(1)
            .SelectMany(_ => Observable.Timer(TimeSpan.FromSeconds(LaunchDelay)))
            .Subscribe(state: launchToast,
                onNext: (_, toast) =>
                {
                    _toastService.DismissToastAsync(toast);
                    _logger.LogInformation("Client launched successfully");
                    IsClientLaunching = false;
                },
                onErrorResume: (ex, _) => _logger.LogError(ex, "Error in process update stream, resuming pipeline"),
                onCompleted: (result, _) => _logger.LogInformation("Process update stream completed with {Status}", result.IsSuccess ? "success" : "failure"))
            .AddTo(_disposables);

        try
        {
            string? pcsx2ExePath = await _pcsx2Locator.FindExeAsync(ct: cts.Token);
            if (pcsx2ExePath is null)
            {
                _logger.LogError("Failed to find PCSX2 executable!");
                await _toastService.InvokeErrorToastAsync("PCSX2 executable not found!");
                return;
            }

            var arguments = $"-fastboot -nofullscreen -- \"{isoPath}\"";
            await _processLauncher.LaunchAsync(pcsx2ExePath, arguments, cts.Token)
                .WaitAsync(TimeSpan.FromSeconds(LaunchTimeout), cts.Token)
                .ConfigureAwait(true);
        }
        catch (TimeoutException) when (IsClientLaunching)
        {
            await _toastService.InvokeErrorToastAsync("PCSX2 client launch timed out!")
                .ConfigureAwait(true);
        }
        catch (OperationCanceledException) when (IsClientLaunching)
        {
            await _toastService.InvokeWarningToastAsync("PCSX2 client launch cancelled!")
                .ConfigureAwait(true);
        }
        finally
        {
            IsClientLaunching = false;
        }
    }

    public void Dispose()
    {
        _disposables.Dispose();
        GC.SuppressFinalize(this);
    }
}
