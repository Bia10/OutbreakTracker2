using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.FileLocators;
using OutbreakTracker2.Application.Services.Launcher;
using OutbreakTracker2.Application.Services.Toasts;
using OutbreakTracker2.PCSX2.Client;
using R3;
using SukiUI.Toasts;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientNotRunning;

public partial class ClientNotRunningViewModel : ObservableObject, IDisposable
{
    private readonly CompositeDisposable _disposables = [];
    private readonly ILogger<ClientNotRunningViewModel> _logger;
    private readonly IToastService _toastService;
    private readonly IProcessLauncher _processLauncher;
    private readonly IPcsx2Locator _pcsx2Locator;
    private readonly IDataManager _dataManager;
    private readonly IGameClientFactory _gameClientFactory;

    private const byte LaunchTimeout = 3;

    [ObservableProperty]
    private bool _isClientLaunching;

    [ObservableProperty]
    private bool _isCancellationRequested;

    public ClientNotRunningViewModel(
        ILogger<ClientNotRunningViewModel> logger,
        IToastService toastService,
        IProcessLauncher processLauncher,
        IPcsx2Locator pcsx2Locator,
        IDataManager dataManager,
        IGameClientFactory gameClientFactory
    )
    {
        _logger = logger;
        _toastService = toastService;
        _processLauncher = processLauncher;
        _pcsx2Locator = pcsx2Locator;
        _dataManager = dataManager;
        _gameClientFactory = gameClientFactory;
    }

    [RelayCommand]
    public async Task LaunchFile1Async()
    {
        string? isoPath = await _pcsx2Locator.FindOutbreakFile1Async().ConfigureAwait(false);
        if (isoPath is null)
        {
            await _toastService.InvokeErrorToastAsync(
                "Outbreak File 1 ISO not found! Please check your game installations.").ConfigureAwait(false);
            return;
        }

        await LaunchGameAsync(isoPath).ConfigureAwait(false);
    }

    [RelayCommand]
    public async Task LaunchFile2Async()
    {
        string? isoPath = await _pcsx2Locator.FindOutbreakFile2Async().ConfigureAwait(false);
        if (isoPath is null)
        {
            await _toastService.InvokeErrorToastAsync(
                "Outbreak File 2 ISO not found! Please check your game installations.").ConfigureAwait(false);
            return;
        }

        await LaunchGameAsync(isoPath).ConfigureAwait(false);
    }

    [RelayCommand]
    public async Task LaunchGameAsync(string isoPath)
    {
        _logger.LogInformation("Launching client");
        IsClientLaunching = true;
        IsCancellationRequested = false;

        CancellationTokenSource cts = new();
        GameClient? gameClient = null;

        _processLauncher.IsCancelling.Subscribe(
            onNext: isCancelling => IsCancellationRequested = isCancelling,
            onErrorResume: ex => _logger.LogError(ex, "Error in IsCancelling observable, resuming pipeline"),
            onCompleted: result => _logger.LogInformation("IsCancelling observable completed with {Status}", result.IsSuccess ? "success" : "failure")
        ).AddTo(_disposables);

        ISukiToast launchToast = _toastService.CreateInfoToastWithCancelButton(
            "Launch client", "Cancel client launch", _ =>
            {
                if (!cts.IsCancellationRequested)
                {
                    _logger.LogInformation("User cancelled client launch via toast");
                    cts.Cancel();
                }
            });

        try
        {
            string? pcsx2ExePath = await _pcsx2Locator.FindExeAsync(ct: cts.Token).ConfigureAwait(false);
            if (pcsx2ExePath is null)
            {
                _logger.LogError("Failed to find PCSX2 executable!");
                await _toastService.InvokeErrorToastAsync("PCSX2 executable not found!").ConfigureAwait(false);
                await _toastService.DismissToastAsync(launchToast).ConfigureAwait(false);
                return;
            }

            string arguments = $"-fastboot -nofullscreen -- \"{isoPath}\"";

            // This pipeline encapsulates:
            // 1. Initializing PCSX2 process via _processLauncher.LaunchAsync.
            // 2. Waiting for _processLauncher.ProcessUpdate to signal PCSX2 is 'running'.
            // 3. Attempting to initialize DataManager which internally waits for EEmemory to be valid.
            Observable<Unit> launchAndInitializePipeline = Observable.FromAsync(async _ =>
                {
                    _logger.LogInformation("Initiating PCSX2 process launch via ProcessLauncher.LaunchAsync");
                    await _processLauncher.LaunchAsync(pcsx2ExePath, arguments, cts.Token).ConfigureAwait(false);
                    _logger.LogInformation("PCSX2 process launched successfully");

                    Process? pcsx2Process = _processLauncher.ClientMonitoredProcess;
                    if (pcsx2Process is null)
                    {
                        _logger.LogError("PCSX2 process not available from ProcessLauncher after launch.");
                        throw new InvalidOperationException("PCSX2 process not available.");
                    }

                    gameClient = await _gameClientFactory.CreateAndAttachGameClientAsync(pcsx2Process, cts.Token).ConfigureAwait(false);
                    _logger.LogInformation("GameClient created and attached successfully.");

                    await _dataManager.InitializeAsync(gameClient, cts.Token).ConfigureAwait(false);
                    _logger.LogInformation("DataManager initialized successfully");

                    return Unit.Default;
                })
                .Take(1);

            launchAndInitializePipeline.Timeout(TimeSpan.FromSeconds(LaunchTimeout))
                .SubscribeAwait(onNextAsync: async (_, _) =>
                    {
                        await _toastService.DismissToastAsync(launchToast).ConfigureAwait(false);
                        _logger.LogInformation("Client launched and data manager initialized successfully");
                        IsClientLaunching = false;
                    }, onErrorResume: async void (innerEx) =>
                    {
                        try
                        {
                            _logger.LogError(innerEx, "Error in client launch/data manager stream");
                            await _toastService.DismissToastAsync(launchToast).ConfigureAwait(false);

                            switch (innerEx)
                            {
                                case TimeoutException:
                                    await _toastService.InvokeErrorToastAsync("PCSX2 client launch and initialization timed out!").ConfigureAwait(false);
                                    break;
                                case OperationCanceledException:
                                    await _toastService.InvokeWarningToastAsync("PCSX2 client launch cancelled!").ConfigureAwait(false);
                                    break;
                                case InvalidOperationException or ArgumentNullException:
                                    await _toastService.InvokeErrorToastAsync($"Setup error: {innerEx.Message}").ConfigureAwait(false);
                                    break;
                                default:
                                    await _toastService.InvokeErrorToastAsync($"An unexpected error occurred: {innerEx.Message}").ConfigureAwait(false);
                                    break;
                            }

                            IsClientLaunching = false;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error in client launch/data manager stream error handler");
                        }
                    }, onCompleted: async void (result) =>
                    {
                        try
                        {
                            _logger.LogInformation("Client launch/data manager stream completed with {Status}", result.IsSuccess ? "success" : "failure");
                            await _toastService.DismissToastAsync(launchToast).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error in client launch/data manager stream completed handler");
                        }
                    }, configureAwait: true, cancelOnCompleted: true
                )
                .AddTo(_disposables);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Client launch cancelled during initial setup");
            await _toastService.InvokeWarningToastAsync("PCSX2 client launch cancelled!").ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            _logger.LogError("PCSX2 client launch timed out during initial setup");
            await _toastService.InvokeErrorToastAsync("PCSX2 client launch timed out!").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during PCSX2 launch setup");
            await _toastService.InvokeErrorToastAsync($"An unexpected error occurred: {ex.Message}").ConfigureAwait(false);
        }
        finally
        {
            IsClientLaunching = false;
            gameClient?.Dispose();
        }
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}