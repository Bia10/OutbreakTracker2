using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using OutbreakTracker2.Application.Services.Embedding;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.EmbeddedGame;

/// <summary>
/// Hosts the PCSX2 game window inside an Avalonia <see cref="NativeControlHost"/>.
/// <para>
/// Platform behaviour:
/// <list type="bullet">
///   <item><description>
///     Linux / WSLg (X11): creates an intermediate X11 container window, then calls
///     <c>XReparentWindow</c> to embed PCSX2 once its window appears.
///   </description></item>
///   <item><description>
///     Windows: creates a STATIC Win32 child window, then calls <c>SetParent</c> to embed PCSX2.
///   </description></item>
/// </list>
/// </para>
/// <para>
/// The DataContext must be an <see cref="EmbeddedGameViewModel"/>.  The view is hidden (via
/// <c>IsVisible</c> binding in the parent XAML) when the platform does not support embedding.
/// </para>
/// </summary>
/// <remarks>
/// <para><b>WSLg / WSL2 testing notes:</b></para>
/// <list type="bullet">
///   <item><description>
///     WSLg sets <c>DISPLAY=:0</c> automatically; no manual configuration is needed.
///   </description></item>
///   <item><description>
///     Avalonia runs on the X11 backend under WSLg (XWayland). GPU acceleration is provided
///     via the WSLg vGPU driver — ensure Windows host GPU drivers are up to date.
///   </description></item>
///   <item><description>
///     PCSX2 must be launched <b>through OutbreakTracker2</b> (not started independently).
///     See <c>Services.Embedding.LinuxWindowEmbedder</c> for the full explanation.
///   </description></item>
///   <item><description>
///     If the window does not embed within 60 s, see the diagnostic commands documented on
///     <c>Services.Embedding.LinuxWindowEmbedder</c>.
///   </description></item>
/// </list>
/// </remarks>
public sealed class EmbeddedGameView : NativeControlHost
{
    // Guarded by the UI thread (CreateNativeControlCore / DestroyNativeControlCore run on UI thread)
    private nint _containerHandle;
    private nint _embeddedHandle;
    private CancellationTokenSource? _searchCts;

    // Created once per operating system; null on non-Windows.
    // Started when embedding succeeds, stopped when de-embedding begins.
    private readonly IWindowPositionWatcher? _positionWatcher;

    /// <summary>
    /// Maximum number of 500 ms poll intervals before giving up on finding the PCSX2 window.
    /// 120 iterations = 60 seconds.
    /// </summary>
    private const int MaxSearchIterations = 120;

    public EmbeddedGameView()
    {
        if (OperatingSystem.IsWindows())
        {
            _positionWatcher = new WindowsWindowPositionWatcher();
            _positionWatcher.EmbeddedWindowLost += OnEmbeddedWindowLost;
        }
    }

    // ── NativeControlHost overrides ──────────────────────────────────────────

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        if (DataContext is not EmbeddedGameViewModel vm)
            return CreateFallbackHandle(parent);

        double scale = VisualRoot?.RenderScaling ?? 1.0;
        int w = PhysicalSize(Bounds.Width, scale);
        int h = PhysicalSize(Bounds.Height, scale);

        _containerHandle = vm.Embedder.CreateContainerWindow(parent.Handle, w, h);

        if (_containerHandle == nint.Zero)
            return CreateFallbackHandle(parent);

        // Embedding is exclusively user-initiated (button click sets IsEmbedRequested=true).
        // If the container is being (re-)created while a request is already active — e.g. the
        // user clicked Embed and Avalonia is just now materialising the NativeControlHost, or
        // the control is being rebuilt after a tab-switch — try an immediate synchronous embed
        // so there is no visible black flash, then fall through to the async poller if the
        // window is not yet available.
        if (vm.IsEmbedRequested && vm.TrackedPid > 0)
        {
            nint found = vm.Embedder.FindProcessWindow(vm.TrackedPid);
            if (found != nint.Zero)
            {
                vm.Embedder.EmbedWindow(_containerHandle, found, w, h);
                _embeddedHandle = found;
                _positionWatcher?.Start(_embeddedHandle, _containerHandle, GetRootWindowHandle());
                vm.IsEmbedded = true;
                vm.IsSearching = false;
                vm.StatusMessage = System.FormattableString.Invariant(
                    $"Embedded HWND 0x{found:X8} (PID {vm.TrackedPid})."
                );
                vm.PropertyChanged += OnViewModelPropertyChanged;

                // The container HWND was just created and hasn't been positioned by
                // Avalonia yet — EmbedWindow used stale screen coordinates.  Post a
                // deferred repaint so the embedded window is repositioned after
                // Avalonia has arranged the container into its final layout slot.
                Dispatcher.UIThread.Post(() => TriggerRepaint(), DispatcherPriority.Render);

                return new PlatformHandle(_containerHandle, parent.HandleDescriptor);
            }

            // Window not found immediately — start async poller.
            BeginWindowSearch(vm);
        }

        // Listen for the user clicking Embed (IsEmbedRequested → true) so we can start the
        // search even when the container was created before the button was pressed.
        vm.PropertyChanged += OnViewModelPropertyChanged;

        return new PlatformHandle(_containerHandle, parent.HandleDescriptor);
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        CancelSearch();

        if (_positionWatcher is not null)
        {
            _positionWatcher.EmbeddedWindowLost -= OnEmbeddedWindowLost;
            _positionWatcher.Stop();
        }

        if (DataContext is EmbeddedGameViewModel vm)
        {
            vm.PropertyChanged -= OnViewModelPropertyChanged;
            vm.ShowDiagnostics = false;

            if (_embeddedHandle != nint.Zero)
            {
                vm.Embedder.ReleaseWindow(_embeddedHandle);
                _embeddedHandle = nint.Zero;
                vm.IsEmbedded = false;
            }

            if (_containerHandle != nint.Zero)
            {
                vm.Embedder.DestroyContainerWindow(_containerHandle);
                _containerHandle = nint.Zero;
            }
        }
    }

    // ── Host-window position tracking is handled by IWindowPositionWatcher ──────────────────
    //
    // The watcher runs a 100 ms background poll that:
    //   * Hides the embedded WS_POPUP window when the container tab is hidden (prevents floating).
    //   * Re-shows and repositions the embedded window when the container becomes visible or moves.
    // OnAttachedToVisualTree / Window.PositionChanged subscription are no longer needed.

    // ── Resize ────────────────────────────────────────────────────────────────

    protected override Size ArrangeOverride(Size finalSize)
    {
        Size arranged = base.ArrangeOverride(finalSize);

        if (_embeddedHandle != nint.Zero && DataContext is EmbeddedGameViewModel vm)
        {
            double scale = VisualRoot?.RenderScaling ?? 1.0;
            int w = PhysicalSize(arranged.Width, scale);
            int h = PhysicalSize(arranged.Height, scale);
            vm.Embedder.ResizeEmbeddedWindow(_embeddedHandle, _containerHandle, w, h);
        }

        return arranged;
    }

    // ── Repaint on tab-switch return ─────────────────────────────────────────

    /// <summary>
    /// Called by <see cref="EmbeddedGameTabView"/> when the user returns to the Game tab.
    /// Avalonia's TabControl keeps this view alive but hidden (IsVisible stays <see langword="true"/>
    /// on <em>this</em> control, but the ancestor <see cref="EmbeddedGameTabView"/> has
    /// its <c>IsVisible</c> toggled false/true).  Because <c>IsEffectivelyVisible</c> is not an
    /// <c>AvaloniaProperty</c> in this version, we cannot detect it via <c>OnPropertyChanged</c>.
    /// The parent calls this method AFTER the Render priority post so the native HWND is already
    /// visible (Avalonia has called <c>ShowWindow(SW_SHOW)</c>) when <c>WM_SIZE</c> is sent.
    /// </summary>
    internal void TriggerRepaint()
    {
        if (_embeddedHandle == nint.Zero || DataContext is not EmbeddedGameViewModel vm)
            return;

        double scale = VisualRoot?.RenderScaling ?? 1.0;
        int w = PhysicalSize(Bounds.Width, scale);
        int h = PhysicalSize(Bounds.Height, scale);
        vm.Embedder.ResizeEmbeddedWindow(_embeddedHandle, _containerHandle, w, h);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is not EmbeddedGameViewModel vm)
            return;

        if (!string.Equals(e.PropertyName, nameof(EmbeddedGameViewModel.IsEmbedRequested), StringComparison.Ordinal))
            return;

        if (vm.IsEmbedRequested)
        {
            // User clicked Embed — start the window search.
            // Automatic embedding on PID detection is intentionally removed; the user must opt in.
            if (vm.TrackedPid > 0 && _embeddedHandle == nint.Zero && _containerHandle != nint.Zero)
                BeginWindowSearch(vm);
        }
        else
        {
            // User clicked Un-embed.
            //
            // IsVisible = false on an Avalonia NativeControlHost does NOT call
            // DestroyNativeControlCore — the container HWND stays alive.  We must therefore
            // release the embedded window here and clear the handle so the next Embed
            // click triggers a fresh search.  The container itself is kept alive so Avalonia
            // still has a valid platform handle.
            //
            // Stop the watcher first and wait for it to drain (bounded by PollIntervalMs * 3)
            // so it cannot call ShowWindow(SW_HIDE) on the handle after ReleaseWindow shows it.
            CancelSearch();
            _positionWatcher?.Stop();

            if (_embeddedHandle != nint.Zero)
            {
                vm.Embedder.ReleaseWindow(_embeddedHandle);
                _embeddedHandle = nint.Zero;
            }

            vm.IsEmbedded = false;
        }
    }

    private void BeginWindowSearch(EmbeddedGameViewModel vm)
    {
        // Cancel any previous search
        CancelSearch();
        _searchCts = new CancellationTokenSource();

        vm.IsSearching = true;
        vm.StatusMessage = "Scanning for PCSX2 window…";

        int pid = vm.TrackedPid;
        IWindowEmbedder embedder = vm.Embedder;
        CancellationToken ct = _searchCts.Token;

        void PostStatus(string msg) =>
            Dispatcher.UIThread.Post(() =>
            {
                if (DataContext is EmbeddedGameViewModel cur)
                    cur.StatusMessage = msg;
            });

        void PostDiag(string diag) =>
            Dispatcher.UIThread.Post(() =>
            {
                if (DataContext is EmbeddedGameViewModel cur)
                    cur.DiagnosticInfo = diag;
            });

        // The poll runs on the thread-pool so we don't block the UI.
        // XInitThreads was called in LinuxWindowEmbedder ctor, so libX11 calls are thread-safe.
        _ = Task.Run(() => PollForWindow(embedder, pid, ct, PostStatus, PostDiag), ct)
            .ContinueWith(
                task =>
                {
                    if (task.IsCanceled)
                        return;

                    if (task.IsFaulted)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            if (DataContext is EmbeddedGameViewModel cur)
                            {
                                cur.StatusMessage = $"Search failed: {task.Exception?.InnerException?.Message}";
                                cur.IsSearching = false;
                                cur.IsEmbedRequested = false;
                            }
                        });
                        return;
                    }

                    nint found = task.Result;

                    if (found == nint.Zero)
                    {
                        // Timed out — surface the diagnostic info and go back to the pre-embed overlay
                        Dispatcher.UIThread.Post(() =>
                        {
                            if (DataContext is EmbeddedGameViewModel cur)
                            {
                                string diag = embedder.GetDiagnosticInfo(pid);
                                cur.StatusMessage = $"Could not find a PCSX2 window for PID {pid} after 60 s.";
                                cur.DiagnosticInfo = diag;
                                cur.LogWarning($"Embed timed out for PCSX2 PID {pid} after 60 s.");
                                cur.IsSearching = false;
                                cur.IsEmbedRequested = false; // return to pre-embed overlay
                            }
                        });
                        return;
                    }

                    // Dispatch back to the UI thread: Avalonia state and embedder calls must
                    // happen there to keep the NativeControlHost lifecycle consistent.
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (_containerHandle == nint.Zero || DataContext is not EmbeddedGameViewModel current)
                            return;

                        // Log what we're about to embed so it's visible in App Log
                        string diag = embedder.GetDiagnosticInfo(pid);
                        current.DiagnosticInfo = diag;

                        double scale = VisualRoot?.RenderScaling ?? 1.0;
                        int w = PhysicalSize(Bounds.Width, scale);
                        int h = PhysicalSize(Bounds.Height, scale);

                        embedder.EmbedWindow(_containerHandle, found, w, h);
                        _embeddedHandle = found;
                        _positionWatcher?.Start(_embeddedHandle, _containerHandle, GetRootWindowHandle());

                        // IsEmbedRequested may still be false when auto-embedding fires before the
                        // user clicked "Embed" (Avalonia creates the NativeControlHost even when the
                        // control is not yet visible).  Setting it true forces EmbeddedGameView.IsVisible
                        // to true, which causes Avalonia to show the container HWND and run
                        // ArrangeOverride, giving the embedded window its correct on-screen size.
                        current.IsEmbedRequested = true;
                        current.IsEmbedded = true;
                        current.IsSearching = false;
                        current.StatusMessage = System.FormattableString.Invariant(
                            $"Embedded HWND 0x{found:X8} (PID {pid})."
                        );

                        // After showing the NativeControlHost, post a repaint at Render priority so
                        // Avalonia has completed ShowWindow on the container before we send WM_SIZE.
                        Dispatcher.UIThread.Post(() => TriggerRepaint(), DispatcherPriority.Render);
                    });
                },
                CancellationToken.None,
                TaskContinuationOptions.None,
                TaskScheduler.Default
            );
    }

    private static nint PollForWindow(
        IWindowEmbedder embedder,
        int pid,
        CancellationToken ct,
        Action<string>? onStatus = null,
        Action<string>? onDiag = null
    )
    {
        for (int i = 0; i < MaxSearchIterations && !ct.IsCancellationRequested; i++)
        {
            // Refresh diagnostics immediately on first try and every 10 iterations (~5 s)
            if (i % 10 == 0)
                onDiag?.Invoke(embedder.GetDiagnosticInfo(pid));

            nint handle = embedder.FindProcessWindow(pid);
            if (handle != nint.Zero)
                return handle;

            int remaining = (MaxSearchIterations - i) / 2;
            onStatus?.Invoke($"Scanning… ({remaining} s remaining)");

            ct.WaitHandle.WaitOne(500);
        }

        return nint.Zero;
    }

    /// <summary>
    /// Called (on the UI thread) when the embedded PCSX2 HWND is destroyed — typically
    /// because PCSX2 recreated its <c>DisplaySurface</c> (renderer change, fullscreen toggle).
    /// Automatically re-discovers the new window and re-embeds it.
    /// </summary>
    private void OnEmbeddedWindowLost(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _embeddedHandle = nint.Zero;

            if (DataContext is EmbeddedGameViewModel vm && vm.IsEmbedRequested && vm.TrackedPid > 0)
            {
                vm.IsEmbedded = false;
                vm.StatusMessage = "PCSX2 window lost \u2014 re-scanning\u2026";
                vm.LogInfo("Embedded HWND was destroyed by PCSX2 (window recreation). Starting re-embed search.");
                BeginWindowSearch(vm);
            }
        });
    }

    private void CancelSearch()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = null;
    }

    /// <summary>
    /// Returns a handle pointing back at the parent so Avalonia has something valid to work with
    /// when the container could not be created (unsupported platform or creation failure).
    /// </summary>
    private static PlatformHandle CreateFallbackHandle(IPlatformHandle parent) =>
        new(parent.Handle, parent.HandleDescriptor);

    /// <summary>
    /// Returns the native HWND of the Avalonia top-level window hosting this control.
    /// Used by the position watcher to detect window-level moves (fullscreen toggle, drag).
    /// </summary>
    private nint GetRootWindowHandle() => TopLevel.GetTopLevel(this)?.TryGetPlatformHandle()?.Handle ?? nint.Zero;

    /// <summary>
    /// Converts a logical (device-independent) pixel value to a physical pixel count using
    /// the current display's render scaling factor, clamped to a minimum of 1.
    /// </summary>
    private static int PhysicalSize(double logical, double scale) => Math.Max(1, (int)(logical * scale));
}
