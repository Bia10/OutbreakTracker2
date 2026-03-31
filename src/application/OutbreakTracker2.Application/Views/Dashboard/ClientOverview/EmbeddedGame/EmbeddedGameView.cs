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

    /// <summary>
    /// Maximum number of 500 ms poll intervals before giving up on finding the PCSX2 window.
    /// 120 iterations = 60 seconds.
    /// </summary>
    private const int MaxSearchIterations = 120;

    // ── NativeControlHost overrides ──────────────────────────────────────────

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        if (DataContext is not EmbeddedGameViewModel vm)
            return CreateFallbackHandle(parent);

        int w = Math.Max(1, (int)Bounds.Width);
        int h = Math.Max(1, (int)Bounds.Height);

        _containerHandle = vm.Embedder.CreateContainerWindow(parent.Handle, w, h);

        if (_containerHandle == nint.Zero)
            return CreateFallbackHandle(parent);

        // If a PID is already known (PCSX2 launched before the tab was shown), search immediately
        if (vm.TrackedPid > 0)
            BeginWindowSearch(vm);

        // Also react when the PID is set later (e.g. user switched to this tab early)
        vm.PropertyChanged += OnViewModelPropertyChanged;

        return new PlatformHandle(_containerHandle, parent.HandleDescriptor);
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        CancelSearch();

        if (DataContext is EmbeddedGameViewModel vm)
        {
            vm.PropertyChanged -= OnViewModelPropertyChanged;

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

    // ── Resize ────────────────────────────────────────────────────────────────

    protected override Size ArrangeOverride(Size finalSize)
    {
        Size arranged = base.ArrangeOverride(finalSize);

        if (_embeddedHandle != nint.Zero && DataContext is EmbeddedGameViewModel vm)
        {
            int w = Math.Max(1, (int)arranged.Width);
            int h = Math.Max(1, (int)arranged.Height);
            vm.Embedder.ResizeEmbeddedWindow(_embeddedHandle, w, h);
        }

        return arranged;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (
            string.Equals(e.PropertyName, nameof(EmbeddedGameViewModel.TrackedPid), StringComparison.Ordinal)
            && sender is EmbeddedGameViewModel vm
            && vm.TrackedPid > 0
            && _embeddedHandle == nint.Zero
            && _containerHandle != nint.Zero
        )
        {
            BeginWindowSearch(vm);
        }
    }

    private void BeginWindowSearch(EmbeddedGameViewModel vm)
    {
        // Cancel any previous search
        CancelSearch();
        _searchCts = new CancellationTokenSource();

        vm.IsSearching = true;

        int pid = vm.TrackedPid;
        IWindowEmbedder embedder = vm.Embedder;
        CancellationToken ct = _searchCts.Token;

        // The poll runs on the thread-pool so we don't block the UI.
        // XInitThreads was called in LinuxWindowEmbedder ctor, so libX11 calls are thread-safe.
        _ = Task.Run(() => PollForWindow(embedder, pid, ct), ct)
            .ContinueWith(
                task =>
                {
                    if (task.IsCanceled || task.IsFaulted)
                        return;

                    nint found = task.Result;
                    if (found == nint.Zero)
                        return;

                    // Dispatch back to the UI thread: Avalonia state and embedder calls must
                    // happen there to keep the NativeControlHost lifecycle consistent.
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (_containerHandle == nint.Zero || DataContext is not EmbeddedGameViewModel current)
                            return;

                        int w = Math.Max(1, (int)Bounds.Width);
                        int h = Math.Max(1, (int)Bounds.Height);

                        embedder.EmbedWindow(_containerHandle, found, w, h);
                        _embeddedHandle = found;
                        current.IsEmbedded = true;
                        current.IsSearching = false;
                    });
                },
                CancellationToken.None,
                TaskContinuationOptions.None,
                TaskScheduler.Default
            );
    }

    private static nint PollForWindow(IWindowEmbedder embedder, int pid, CancellationToken ct)
    {
        for (int i = 0; i < MaxSearchIterations && !ct.IsCancellationRequested; i++)
        {
            nint handle = embedder.FindProcessWindow(pid);
            if (handle != nint.Zero)
                return handle;

            ct.WaitHandle.WaitOne(500);
        }

        return nint.Zero;
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
}
