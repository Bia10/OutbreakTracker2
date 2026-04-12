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

namespace OutbreakTracker2.Application.Views.Dashboard.ClientNotRunning;

public sealed partial class ClientNotRunningViewModel(
    ILogger<ClientNotRunningViewModel> logger,
    IToastService toastService,
    IProcessLauncher processLauncher,
    IPcsx2Locator pcsx2Locator,
    IDataManager dataManager
) : ObservableObject, IDisposable
{
    private readonly ILogger<ClientNotRunningViewModel> _logger = logger;
    private readonly IToastService _toastService = toastService;
    private readonly IProcessLauncher _processLauncher = processLauncher;
    private readonly IPcsx2Locator _pcsx2Locator = pcsx2Locator;
    private readonly IDataManager _dataManager = dataManager;

    // Tracks the IsCancelling subscription for the current launch attempt. Disposed before
    // each new launch to prevent subscriptions accumulating on repeated LaunchGameAsync calls.
    private IDisposable? _isCancellingSubscription;
    private CancellationTokenSource? _launchCancellationTokenSource;

    private const byte LaunchTimeout = 3;

    [ObservableProperty]
    private bool _isClientLaunching;

    [ObservableProperty]
    private bool _isCancellationRequested;

    [RelayCommand]
    public async Task LaunchFile1Async()
    {
        string? isoPath = await _pcsx2Locator.FindOutbreakFile1Async().ConfigureAwait(false);
        if (isoPath is null)
        {
            await _toastService
                .InvokeErrorToastAsync("Outbreak File 1 ISO not found! Please check your game installations.")
                .ConfigureAwait(false);
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
            await _toastService
                .InvokeErrorToastAsync("Outbreak File 2 ISO not found! Please check your game installations.")
                .ConfigureAwait(false);
            return;
        }

        await LaunchGameAsync(isoPath).ConfigureAwait(false);
    }

    [RelayCommand]
    public async Task LaunchGameAsync(string isoPath)
    {
        if (IsClientLaunching)
        {
            _logger.LogInformation("Ignoring duplicate launch request while a launch is already in progress");
            return;
        }

        _logger.LogInformation("Launching client");
        IsClientLaunching = true;
        IsCancellationRequested = false;

        CancellationTokenSource launchCts = new();
        _launchCancellationTokenSource = launchCts;

        // Dispose any previous IsCancelling subscription before registering a new one
        // to prevent subscriptions accumulating if the user triggers multiple launches.
        _isCancellingSubscription?.Dispose();
        _isCancellingSubscription = _processLauncher.IsCancelling.Subscribe(
            onNext: isCancelling => IsCancellationRequested = isCancelling,
            onErrorResume: ex => _logger.LogError(ex, "Error in IsCancelling observable, resuming pipeline"),
            onCompleted: result =>
                _logger.LogInformation(
                    "IsCancelling observable completed with {Status}",
                    result.IsSuccess ? "success" : "failure"
                )
        );

        ISukiToast launchToast = _toastService.CreateInfoToastWithCancelButton(
            "Launch client",
            "Cancel client launch",
            _ =>
            {
                try
                {
                    if (!launchCts.IsCancellationRequested)
                    {
                        _logger.LogInformation("User cancelled client launch via toast");
                        IsCancellationRequested = true;
                        launchCts.Cancel();
                    }
                }
                catch (ObjectDisposedException) { }
            }
        );

        try
        {
            string? pcsx2ExePath = await _pcsx2Locator.FindExeAsync(ct: launchCts.Token).ConfigureAwait(false);
            if (pcsx2ExePath is null)
            {
                _logger.LogError("Failed to find PCSX2 executable!");
                await _toastService.InvokeErrorToastAsync("PCSX2 executable not found!").ConfigureAwait(false);
                await _toastService.DismissToastAsync(launchToast).ConfigureAwait(false);
                return;
            }

            string arguments = $"-fastboot -nofullscreen -- \"{isoPath}\"";
            using CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(LaunchTimeout));
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                launchCts.Token,
                timeoutCts.Token
            );

            try
            {
                await LaunchAndInitializeAsync(pcsx2ExePath, arguments, linkedCts.Token).ConfigureAwait(false);
                await _toastService.DismissToastAsync(launchToast).ConfigureAwait(false);
                _logger.LogInformation("Client launch/data manager stream completed with success");
            }
            catch (OperationCanceledException)
                when (timeoutCts.IsCancellationRequested && !launchCts.IsCancellationRequested)
            {
                _logger.LogError("PCSX2 client launch timed out during launch/data-manager initialization");
                await _toastService.DismissToastAsync(launchToast).ConfigureAwait(false);
                await _toastService
                    .InvokeErrorToastAsync("PCSX2 client launch and initialization timed out!")
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (launchCts.IsCancellationRequested)
            {
                _logger.LogInformation("Client launch cancelled during launch/data-manager initialization");
                await _toastService.DismissToastAsync(launchToast).ConfigureAwait(false);
                await _toastService.InvokeWarningToastAsync("PCSX2 client launch cancelled!").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in client launch/data manager stream");
                await _toastService.DismissToastAsync(launchToast).ConfigureAwait(false);

                Task toastTask = ex switch
                {
                    InvalidOperationException or ArgumentNullException => _toastService.InvokeErrorToastAsync(
                        $"Setup error: {ex.Message}"
                    ),
                    _ => _toastService.InvokeErrorToastAsync($"An unexpected error occurred: {ex.Message}"),
                };
                await toastTask.ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (launchCts.IsCancellationRequested)
        {
            _logger.LogInformation("Client launch cancelled during initial setup");
            await _toastService.DismissToastAsync(launchToast).ConfigureAwait(false);
            await _toastService.InvokeWarningToastAsync("PCSX2 client launch cancelled!").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during PCSX2 launch setup");
            await _toastService.DismissToastAsync(launchToast).ConfigureAwait(false);
            await _toastService
                .InvokeErrorToastAsync($"An unexpected error occurred: {ex.Message}")
                .ConfigureAwait(false);
        }
        finally
        {
            if (ReferenceEquals(_launchCancellationTokenSource, launchCts))
                _launchCancellationTokenSource = null;

            launchCts.Dispose();
            IsClientLaunching = false;
            IsCancellationRequested = false;
        }
    }

    private async Task LaunchAndInitializeAsync(
        string pcsx2ExePath,
        string arguments,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInformation("Initiating PCSX2 process launch via ProcessLauncher.LaunchAsync");
        await _processLauncher.LaunchAsync(pcsx2ExePath, arguments, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("PCSX2 process launched successfully");

        IGameClient gameClient =
            _processLauncher.GetActiveGameClient()
            ?? throw new InvalidOperationException("GameClient not available after launch.");
        _logger.LogInformation("GameClient acquired successfully.");

        await _dataManager.InitializeAsync(gameClient, cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        _launchCancellationTokenSource?.Cancel();
        _launchCancellationTokenSource = null;
        _isCancellingSubscription?.Dispose();
    }
}
