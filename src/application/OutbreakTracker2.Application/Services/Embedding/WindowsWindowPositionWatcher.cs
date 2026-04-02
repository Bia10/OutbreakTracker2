using System.Runtime.Versioning;
using OutbreakTracker2.WinInterop;

namespace OutbreakTracker2.Application.Services.Embedding;

/// <summary>
/// Windows implementation of <see cref="IWindowPositionWatcher"/>.
/// Runs a background poll every <see cref="PollIntervalMs"/> milliseconds and:
/// <list type="bullet">
///   <item><description>
///     Hides the embedded window when the container is hidden (prevents WS_POPUP windows from
///     floating over unrelated UI during tab switches).
///   </description></item>
///   <item><description>
///     Repositions and shows the embedded window when the container becomes visible or moves.
///     This covers: host-window drag, host-window resize, tab-switch return, Avalonia layout change.
///   </description></item>
///   <item><description>
///     Stops automatically if either handle is destroyed (e.g. PCSX2 exits).
///   </description></item>
/// </list>
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsWindowPositionWatcher : IWindowPositionWatcher
{
    private CancellationTokenSource? _cts;
    private Task? _watchTask;
    private const int PollIntervalMs = 100;

    public void Start(nint embeddedHandle, nint containerHandle)
    {
        Stop();
        _cts = new CancellationTokenSource();
        _watchTask = WatchAsync(embeddedHandle, containerHandle, _cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        // Wait for the current poll iteration to drain so the watcher cannot call
        // ShowWindow(SW_HIDE) on the embedded handle after ReleaseWindow has shown it.
        // PollIntervalMs * 3 is a safe upper bound; once the CTS is cancelled the
        // Task.Delay throws immediately and the task exits in microseconds.
        _watchTask?.Wait(millisecondsTimeout: PollIntervalMs * 3);
        _cts?.Dispose();
        _cts = null;
        _watchTask = null;
    }

    private static async Task WatchAsync(nint embedded, nint container, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(PollIntervalMs, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            // Guard: cancellation may have been requested while we awaited the delay.
            if (ct.IsCancellationRequested)
                return;

            // Stop watching if either window was destroyed (PCSX2 exited, container torn down, etc.)
            if (!Win32WindowNativeMethods.IsWindow(container) || !Win32WindowNativeMethods.IsWindow(embedded))
                return;

            bool containerVisible = Win32WindowNativeMethods.IsWindowVisible(container);
            bool embeddedVisible = Win32WindowNativeMethods.IsWindowVisible(embedded);

            // ── Container hidden (tab switched away) ──────────────────────────
            // WS_POPUP windows are not automatically hidden when their Win32 parent/owner is
            // hidden — they float freely over whatever is on screen.  Mirror the container's
            // hidden state so the game window disappears with the tab.
            if (!containerVisible)
            {
                if (embeddedVisible)
                    Win32WindowNativeMethods.ShowWindow(embedded, Win32WindowNativeMethods.SW_HIDE);

                continue;
            }

            // ── Container visible — sync position ─────────────────────────────
            if (!Win32WindowNativeMethods.GetWindowRect(container, out RECT cr))
                continue;

            if (!Win32WindowNativeMethods.GetWindowRect(embedded, out RECT er))
                continue;

            bool positionDrifted =
                er.Left != cr.Left || er.Top != cr.Top || er.Width != cr.Width || er.Height != cr.Height;

            if (positionDrifted)
                Win32WindowNativeMethods.MoveWindow(embedded, cr.Left, cr.Top, cr.Width, cr.Height, bRepaint: true);

            // ── Ensure embedded window is shown and its swap chain is refreshed ──
            if (!embeddedVisible)
            {
                Win32WindowNativeMethods.ShowWindow(embedded, Win32WindowNativeMethods.SW_SHOW);

                // WM_SIZE forces Qt/PCSX2 to resize and repaint its D3D/GL render target.
                nint lParam = (nint)(((cr.Height & 0xFFFF) << 16) | (cr.Width & 0xFFFF));
                Win32WindowNativeMethods.PostMessage(
                    embedded,
                    Win32WindowNativeMethods.WM_SIZE,
                    Win32WindowNativeMethods.SIZE_RESTORED,
                    lParam
                );
            }
        }
    }

    public void Dispose() => Stop();
}
