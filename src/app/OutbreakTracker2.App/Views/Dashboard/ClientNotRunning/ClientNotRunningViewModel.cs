using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.App.Services.Data;
using OutbreakTracker2.App.Services.FileLocators;
using OutbreakTracker2.App.Services.ProcessLauncher;
using OutbreakTracker2.App.Services.Toasts;
using OutbreakTracker2.PCSX2;
using R3;
using SukiUI.Toasts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OutbreakTracker2.App.Views.Dashboard.ClientNotRunning;

public partial class ClientNotRunningViewModel : ObservableObject, IDisposable
{
    private readonly CompositeDisposable _disposables = [];
    private readonly ILogger<ClientNotRunningViewModel> _logger;
    private readonly IToastService _toastService;
    private readonly IProcessLauncher _processLauncher;
    private readonly IPcsx2Locator _pcsx2Locator;
    private readonly IDataManager _dataManager;

    // Overall timeout for launch and data manager init (increased to allow for EEmemory retries)
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
        IDataManager dataManager
    )
    {
        _logger = logger;
        _toastService = toastService;
        _processLauncher = processLauncher;
        _pcsx2Locator = pcsx2Locator;
        _dataManager = dataManager;
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
        IsCancellationRequested = false;

        CancellationTokenSource cts = new();

        _processLauncher.IsCancelling.Subscribe(
            onNext: isCancelling => IsCancellationRequested = isCancelling,
            onErrorResume: ex => _logger.LogError(ex, "Error in IsCancelling observable, resuming pipeline"),
            onCompleted: result => _logger.LogInformation("IsCancelling observable completed with {Status}", result.IsSuccess ? "success" : "failure")
        ).AddTo(_disposables);

        ISukiToast launchToast = _toastService.CreateInfoToastWithCancelButton(
            "Launch client", "Cancel client launch", toast =>
            {
                if (!cts.IsCancellationRequested)
                {
                    _logger.LogInformation("User cancelled client launch via toast");
                    cts.Cancel();
                }
            });

        try
        {
            string? pcsx2ExePath = await _pcsx2Locator.FindExeAsync(ct: cts.Token);
            if (pcsx2ExePath is null)
            {
                _logger.LogError("Failed to find PCSX2 executable!");
                await _toastService.InvokeErrorToastAsync("PCSX2 executable not found!");
                IsClientLaunching = false;
                await _toastService.DismissToastAsync(launchToast);
                cts.Dispose();
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
                    await _processLauncher.LaunchAsync(pcsx2ExePath, arguments, cts.Token);
                    _logger.LogInformation("PCSX2 process launched successfully");
                    return true;
                })
                .Where(launchedSuccessfully => launchedSuccessfully)
                .Take(1)
                .SelectMany(_ => Observable.FromAsync(async _ =>
                    {
                        _logger.LogInformation("PCSX2 process reported as running. Attempting to initialize DataManager (EEmemory connection)");

                        GameClient? gameClient = _processLauncher.GetActiveGameClient();
                        if (gameClient is null)
                        {
                            _logger.LogError("GameClient not available from ProcessLauncher after PCSX2 launch. Cannot initialize DataManager");
                            throw new InvalidOperationException("GameClient not available after PCSX2 launch.");
                        }

                        await _dataManager.InitializeAsync(gameClient, cts.Token);
                        _logger.LogInformation("DataManager initialized successfully");
                    })
                );

            launchAndInitializePipeline.Timeout(TimeSpan.FromSeconds(LaunchTimeout))
                .SubscribeAwait(onNextAsync: async (success, token) =>
                    {
                        await _toastService.DismissToastAsync(launchToast);
                        _logger.LogInformation("Client launched and data manager initialized successfully");
                        IsClientLaunching = false;
                        cts.Dispose();
                    }, onErrorResume: async void (innerEx) =>
                    {
                        try
                        {
                            _logger.LogError(innerEx, "Error in client launch/data manager stream");

                            await _toastService.DismissToastAsync(launchToast);

                            switch (innerEx)
                            {
                                case TimeoutException:
                                    await _toastService.InvokeErrorToastAsync("PCSX2 client launch and initialization timed out!");
                                    break;
                                case OperationCanceledException:
                                    await _toastService.InvokeWarningToastAsync("PCSX2 client launch cancelled!");
                                    break;
                                case InvalidOperationException or ArgumentNullException:
                                    await _toastService.InvokeErrorToastAsync($"Setup error: {innerEx.Message}");
                                    break;
                                default:
                                    await _toastService.InvokeErrorToastAsync($"An unexpected error occurred: {innerEx.Message}");
                                    break;
                            }

                            IsClientLaunching = false;
                            cts.Dispose();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error in client launch/data manager stream error handler");
                            throw;
                        }
                    }, onCompleted: async void (result) =>
                    {
                        try
                        {
                            _logger.LogInformation("Client launch/data manager stream completed with {Status}", result.IsSuccess ? "success" : "failure");
                            await _toastService.DismissToastAsync(launchToast);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error in client launch/data manager stream completed handler");
                            throw; // TODO handle exception
                        }
                    }, configureAwait: true, cancelOnCompleted: true
                ).AddTo(_disposables);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Client launch cancelled during initial setup");
            await _toastService.InvokeWarningToastAsync("PCSX2 client launch cancelled!");
            IsClientLaunching = false;
            await _toastService.DismissToastAsync(launchToast);
            cts.Dispose();
        }
        catch (TimeoutException)
        {
            _logger.LogError("PCSX2 client launch timed out during initial setup");
            await _toastService.InvokeErrorToastAsync("PCSX2 client launch timed out!");
            IsClientLaunching = false;
            await _toastService.DismissToastAsync(launchToast);
            cts.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during PCSX2 launch setup");
            await _toastService.InvokeErrorToastAsync($"An unexpected error occurred: {ex.Message}");
            IsClientLaunching = false;
            await _toastService.DismissToastAsync(launchToast);
            cts.Dispose();
        }
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}
