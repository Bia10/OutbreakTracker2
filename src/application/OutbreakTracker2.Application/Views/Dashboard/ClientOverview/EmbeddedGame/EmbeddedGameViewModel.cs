using System;
using CommunityToolkit.Mvvm.ComponentModel;
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
    /// <see langword="true"/> when window embedding is available on the current platform.
    /// Bound by the view to toggle the NativeControlHost vs. the unsupported-platform message.
    /// </summary>
    public bool IsSupported { get; }

    /// <summary>Inverse of <see cref="IsSupported"/> — used by XAML bindings.</summary>
    public bool IsNotSupported => !IsSupported;

    public EmbeddedGameViewModel(
        IWindowEmbedder embedder,
        IProcessLauncher processLauncher,
        ILogger<EmbeddedGameViewModel> logger
    )
    {
        Embedder = embedder ?? throw new ArgumentNullException(nameof(embedder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        IsSupported = embedder.IsSupported;

        if (!IsSupported)
            _logger.LogInformation(
                "Window embedding is not supported on this platform (no X11 display or unsupported OS)."
            );

        // Subscribe to process lifecycle so we know which PID to look for
        _processSubscription = processLauncher.ProcessUpdate.Subscribe(model =>
        {
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
            }
        });
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
