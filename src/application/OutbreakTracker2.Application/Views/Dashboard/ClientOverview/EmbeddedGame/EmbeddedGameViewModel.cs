using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Services.Embedding;
using OutbreakTracker2.Application.Services.Launcher;
using R3;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.EmbeddedGame;

/// <summary>
/// ViewModel for the embedded PCSX2 game surface tab.
/// Tracks embedding state and exposes the <see cref="IWindowEmbedder"/> to the paired view.
/// </summary>
public partial class EmbeddedGameViewModel : ObservableObject, IDisposable
{
    private readonly ILogger<EmbeddedGameViewModel> _logger;
    private readonly IDisposable _processSubscription;
    private bool _disposed;

    /// <summary>Platform-specific embedder used by <see cref="EmbeddedGameView"/>.</summary>
    internal IWindowEmbedder Embedder { get; }

    /// <summary>PID of the monitored PCSX2 process. Zero when no process is running.</summary>
    [ObservableProperty]
    private int _trackedPid;

    /// <summary>
    /// <see langword="true"/> when the PCSX2 window has been successfully reparented into the host.
    /// </summary>
    [ObservableProperty]
    private bool _isEmbedded;

    /// <summary><see langword="true"/> while polling for the PCSX2 window to appear.</summary>
    [ObservableProperty]
    private bool _isSearching;

    /// <summary>
    /// <see langword="true"/> after the user has clicked "Embed PCSX2".
    /// Controls the visibility of the <see cref="EmbeddedGameView"/> NativeControlHost so the
    /// HWND container is only created on demand (avoids z-order conflicts with the overlay).
    /// Reset to <see langword="false"/> when the process exits or embedding times out.
    /// </summary>
    [ObservableProperty]
    private bool _isEmbedRequested;

    /// <summary>
    /// Human-readable status shown in the pre-embed overlay (e.g. "Waiting for PCSX2…",
    /// "Scanning…", "Timed out — try again").
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = "PCSX2 is not running.";

    /// <summary>
    /// Detailed window enumeration results for debugging (populated after each scan attempt).
    /// Empty when there is nothing to report.
    /// </summary>
    [ObservableProperty]
    private string _diagnosticInfo = string.Empty;

    /// <summary>
    /// <see langword="true"/> when window embedding is available on the current platform.
    /// Bound by the view to toggle the NativeControlHost vs. the unsupported-platform message.
    /// </summary>
    public bool IsSupported { get; }

    /// <summary>Inverse of <see cref="IsSupported"/> — used by XAML bindings.</summary>
    public bool IsNotSupported => !IsSupported;

    /// <summary>
    /// <see langword="true"/> when the pre-embed overlay (status + button) should be shown.
    /// Equivalent to <c>IsSupported &amp;&amp; !IsEmbedRequested</c>.
    /// </summary>
    public bool IsPreEmbedState => IsSupported && !IsEmbedRequested;

    public EmbeddedGameViewModel(
        IWindowEmbedder embedder,
        IProcessLauncher processLauncher,
        ILogger<EmbeddedGameViewModel> logger
    )
    {
        Embedder = embedder ?? throw new ArgumentNullException(nameof(embedder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        IsSupported = embedder.IsSupported;

        _logger.LogInformation(
            "EmbeddedGameViewModel constructed. IsSupported={IsSupported}, EmbedderType={EmbedderType}",
            IsSupported,
            embedder.GetType().Name
        );

        if (!IsSupported)
            _logger.LogInformation(
                "Window embedding is not supported on this platform (no X11 display or unsupported OS)."
            );

        // Subscribe to process lifecycle so we know which PID to look for
        _processSubscription = processLauncher.ProcessUpdate.Subscribe(model =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                _logger.LogInformation(
                    "ProcessUpdate received — IsRunning={IsRunning}, PID={Pid}",
                    model.IsRunning,
                    model.Id
                );

                if (model.IsRunning)
                {
                    TrackedPid = model.Id;
                    _logger.LogInformation("Tracking PCSX2 PID {Pid} for window embedding.", model.Id);
                }
                else
                {
                    TrackedPid = 0;
                    IsEmbedded = false;
                    IsSearching = false;
                    IsEmbedRequested = false;
                    DiagnosticInfo = string.Empty;
                }
            });
        });
    }

    /// <summary>Requests that the <see cref="EmbeddedGameView"/> capture the PCSX2 window.</summary>
    [RelayCommand(CanExecute = nameof(CanRequestEmbed))]
    private void RequestEmbed()
    {
        DiagnosticInfo = string.Empty;
        IsEmbedRequested = true;
    }

    private bool CanRequestEmbed() => TrackedPid > 0 && !IsEmbedRequested;

    /// <summary>Releases the embedded PCSX2 window back to a standalone window.</summary>
    [RelayCommand(CanExecute = nameof(CanRequestUnembed))]
    private void RequestUnembed()
    {
        // Setting IsEmbedRequested = false collapses the NativeControlHost visibility,
        // which causes Avalonia to call DestroyNativeControlCore. That path already stops
        // the position watcher, releases the window and destroys the container — no
        // duplicated work needed here.
        IsEmbedRequested = false;
        IsEmbedded = false;
        StatusMessage =
            TrackedPid > 0
                ? $"PCSX2 detected (PID {TrackedPid}). Click \"Embed PCSX2\" to capture the game display."
                : "PCSX2 is not running.";
    }

    private bool CanRequestUnembed() => IsEmbedded;

    /// <summary>Label shown on the embed/un-embed toggle button.</summary>
    public string EmbedButtonContent => IsEmbedded ? "Un-embed" : "Embed PCSX2";

    /// <summary>Command bound to the embed/un-embed toggle button.</summary>
    public System.Windows.Input.ICommand EmbedButtonCommand => IsEmbedded ? RequestUnembedCommand : RequestEmbedCommand;

    /// <summary>Toggles the diagnostics panel visibility. Refreshes data when opening.</summary>
    [RelayCommand]
    private void ToggleDiagnostics()
    {
        if (!ShowDiagnostics && TrackedPid > 0)
            DiagnosticInfo = Embedder.GetDiagnosticInfo(TrackedPid);
        ShowDiagnostics = !ShowDiagnostics;
    }

    /// <summary><see langword="true"/> when the diagnostics panel is expanded.</summary>
    [ObservableProperty]
    private bool _showDiagnostics;

    /// <summary>Forwards a message to the logger so it appears in the App Log tab.</summary>
    internal void LogInfo(string message) => _logger.LogInformation("{Msg}", message);

    partial void OnTrackedPidChanged(int value)
    {
        if (value > 0)
        {
            StatusMessage = $"PCSX2 detected (PID {value}). Click \"Embed PCSX2\" to capture the game display.";

            // Run an immediate window scan so diagnostics are populated before the user
            // clicks anything.  This also writes to ILogger so App Log shows the result.
            string diag = Embedder.GetDiagnosticInfo(value);
            DiagnosticInfo = diag;
            _logger.LogInformation("PCSX2 PID {Pid} detected. Window scan:\n{Diag}", value, diag);
        }
        else
        {
            StatusMessage = "PCSX2 is not running.";
            DiagnosticInfo = string.Empty;
            ShowDiagnostics = false;
        }

        RequestEmbedCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsEmbedRequestedChanged(bool value)
    {
        OnPropertyChanged(nameof(IsPreEmbedState));
        RequestEmbedCommand.NotifyCanExecuteChanged();
        RequestUnembedCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(EmbedButtonContent));
        OnPropertyChanged(nameof(EmbedButtonCommand));
    }

    partial void OnIsEmbeddedChanged(bool value)
    {
        RequestUnembedCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(EmbedButtonContent));
        OnPropertyChanged(nameof(EmbedButtonCommand));
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _processSubscription.Dispose();
        ((IDisposable)Embedder).Dispose();
        _disposed = true;
    }
}
