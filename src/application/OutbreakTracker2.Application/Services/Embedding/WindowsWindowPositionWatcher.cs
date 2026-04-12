using System.Runtime.Versioning;
using OutbreakTracker2.WinInterop;

namespace OutbreakTracker2.Application.Services.Embedding;

/// <summary>
/// Windows implementation of <see cref="IWindowPositionWatcher"/>.
/// <para>
/// <b>100% event-driven</b> — no polling of any kind.
/// </para>
/// <list type="bullet">
///   <item><description>
///     <c>EVENT_OBJECT_LOCATIONCHANGE</c> on the <b>container</b> (own process) → repositions
///     the embedded WS_POPUP PCSX2 window to match the container's screen rect instantly.
///   </description></item>
///   <item><description>
///     <c>EVENT_OBJECT_LOCATIONCHANGE</c> on the <b>embedded</b> (PCSX2 process) → snaps the
///     PCSX2 window back when Qt moves it on its own (e.g. after <c>WM_STYLECHANGED</c>
///     recalculation or internal layout adjustments).
///   </description></item>
///   <item><description>
///     <c>EVENT_OBJECT_SHOW</c> / <c>EVENT_OBJECT_HIDE</c> → mirrors container visibility.
///   </description></item>
///   <item><description>
///     <c>EVENT_OBJECT_DESTROY</c> on the embedded HWND → fires
///     <see cref="EmbeddedWindowLost"/> so the view can re-discover and re-embed the new
///     PCSX2 window (PCSX2 recreates its <c>DisplaySurface</c> on renderer / fullscreen changes).
///   </description></item>
///   <item><description>
///     <c>WH_MOUSE_LL</c> low-level mouse hook → routes keyboard focus to PCSX2 the instant
///     the cursor enters the game area and returns it to Avalonia when the cursor leaves.
///     Replaces the former 500ms focus poll.
///   </description></item>
/// </list>
/// <remarks>
/// Both <see cref="Start"/> and <see cref="Stop"/> <b>must</b> be called from the Avalonia
/// UI thread — <c>SetWinEventHook</c>, <c>UnhookWinEvent</c>, <c>SetWindowsHookEx</c>,
/// and <c>UnhookWindowsHookEx</c> must all be called from the same thread.
/// </remarks>
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsWindowPositionWatcher : IWindowPositionWatcher
{
    // ── Public event ──────────────────────────────────────────────────────────

    public event EventHandler? EmbeddedWindowLost;

    // ── WinEvent hook state ───────────────────────────────────────────────────
    // Delegate fields MUST be stored to prevent GC collection while hooks are active.

    private nint _locationHook; // container location change (own PID)
    private nint _visibilityHook; // container show/hide (own PID)
    private nint _snapBackHook; // embedded location change (PCSX2 PID)
    private nint _destroyHook; // embedded destroy (PCSX2 PID)
    private nint _foregroundHook; // OT2 window gained foreground (own PID) — alt-tab return

    private Win32WindowNativeMethods.WinEventProc? _locationCallback;
    private Win32WindowNativeMethods.WinEventProc? _visibilityCallback;
    private Win32WindowNativeMethods.WinEventProc? _snapBackCallback;
    private Win32WindowNativeMethods.WinEventProc? _destroyCallback;
    private Win32WindowNativeMethods.WinEventProc? _foregroundCallback;

    // ── Low-level mouse hook state ────────────────────────────────────────────

    private nint _mouseHook;
    private Win32WindowNativeMethods.LowLevelMouseProc? _mouseCallback;

    // ── Shared state ──────────────────────────────────────────────────────────

    private nint _embedded;
    private nint _container;
    private nint _rootWindow; // Avalonia top-level window (GA_ROOT of container)
    private uint _uiThreadId;
    private uint _pcsx2ThreadId;
    private bool _inputAttached;

    // ── Cached container rect (updated by location hooks, read by mouse hook) ──

    private RECT _cachedContainerRect;
    private bool _focusIsOnEmbedded;

    // ── IWindowPositionWatcher ────────────────────────────────────────────────

    public void Start(nint embeddedHandle, nint containerHandle, nint rootWindowHandle)
    {
        Stop();

        _embedded = embeddedHandle;
        _container = containerHandle;
        _rootWindow = rootWindowHandle;
        _uiThreadId = Win32WindowNativeMethods.GetCurrentThreadId();

        // Get PCSX2 process and thread IDs from the embedded HWND.
        _pcsx2ThreadId = Win32WindowNativeMethods.GetWindowThreadProcessId(_embedded, out uint pcsx2Pid);

        // Attach the UI thread's input queue to the PCSX2 thread so cross-process
        // SetFocus calls work.
        _inputAttached =
            _pcsx2ThreadId != 0
            && _pcsx2ThreadId != _uiThreadId
            && Win32WindowNativeMethods.AttachThreadInput(_uiThreadId, _pcsx2ThreadId, fAttach: true);

        // Seed the cached container rect for the mouse hook.
        if (Win32WindowNativeMethods.GetWindowRect(_container, out RECT initialRect))
            _cachedContainerRect = initialRect;

        // ── Hook 1: container position / size (own process) ───────────────────
        _locationCallback = OnContainerLocationChanged;
        _locationHook = Win32WindowNativeMethods.SetWinEventHook(
            Win32WindowNativeMethods.EVENT_OBJECT_LOCATIONCHANGE,
            Win32WindowNativeMethods.EVENT_OBJECT_LOCATIONCHANGE,
            nint.Zero,
            _locationCallback,
            (uint)Environment.ProcessId,
            0,
            Win32WindowNativeMethods.WINEVENT_OUTOFCONTEXT
        );

        // ── Hook 2: container show / hide (own process) ───────────────────────
        _visibilityCallback = OnVisibilityChanged;
        _visibilityHook = Win32WindowNativeMethods.SetWinEventHook(
            Win32WindowNativeMethods.EVENT_OBJECT_SHOW,
            Win32WindowNativeMethods.EVENT_OBJECT_HIDE,
            nint.Zero,
            _visibilityCallback,
            (uint)Environment.ProcessId,
            0,
            Win32WindowNativeMethods.WINEVENT_OUTOFCONTEXT
        );

        // ── Hook 3: embedded snap-back (PCSX2 process) ───────────────────────
        // PCSX2's Qt framework may move the window on its own (e.g. after
        // WM_STYLECHANGED triggers frame recalculation, or internal layout changes).
        // This hook detects any drift and snaps the window back instantly.
        _snapBackCallback = OnEmbeddedLocationChanged;
        _snapBackHook = Win32WindowNativeMethods.SetWinEventHook(
            Win32WindowNativeMethods.EVENT_OBJECT_LOCATIONCHANGE,
            Win32WindowNativeMethods.EVENT_OBJECT_LOCATIONCHANGE,
            nint.Zero,
            _snapBackCallback,
            pcsx2Pid,
            0,
            Win32WindowNativeMethods.WINEVENT_OUTOFCONTEXT
        );

        // ── Hook 4: embedded destroy (PCSX2 process) ─────────────────────────
        // PCSX2 destroys and recreates its DisplaySurface QWindow when changing
        // renderer, toggling fullscreen, or modifying display settings.  When the
        // embedded HWND vanishes, we fire EmbeddedWindowLost so the view can
        // re-discover the new window and re-embed it.
        _destroyCallback = OnEmbeddedDestroyed;
        _destroyHook = Win32WindowNativeMethods.SetWinEventHook(
            Win32WindowNativeMethods.EVENT_OBJECT_DESTROY,
            Win32WindowNativeMethods.EVENT_OBJECT_DESTROY,
            nint.Zero,
            _destroyCallback,
            pcsx2Pid,
            0,
            Win32WindowNativeMethods.WINEVENT_OUTOFCONTEXT
        );

        // ── Hook 5: OT2 foreground activation (own process) ──────────────────
        // EVENT_SYSTEM_FOREGROUND fires exactly when an OT2-owned window becomes
        // the foreground window — including every alt-tab return.  PCSX2 runs in a
        // different process so this hook never fires for PCSX2 activity.
        //
        // On activation: reset the stale _focusIsOnEmbedded flag (the OS already
        // moved focus to Avalonia), then immediately re-route focus to PCSX2 if
        // the cursor is already inside the game area — fixing the "must wiggle the
        // mouse to get keyboard control back" symptom.
        _foregroundCallback = OnForegroundChanged;
        _foregroundHook = Win32WindowNativeMethods.SetWinEventHook(
            Win32WindowNativeMethods.EVENT_SYSTEM_FOREGROUND,
            Win32WindowNativeMethods.EVENT_SYSTEM_FOREGROUND,
            nint.Zero,
            _foregroundCallback,
            (uint)Environment.ProcessId,
            0,
            Win32WindowNativeMethods.WINEVENT_OUTOFCONTEXT
        );

        // ── Hook 6: low-level mouse hook for instant focus routing ────────────
        _mouseCallback = OnMouseEvent;
        nint hModule = Win32WindowNativeMethods.GetModuleHandle(null);
        _mouseHook = Win32WindowNativeMethods.SetWindowsHookEx(
            Win32WindowNativeMethods.WH_MOUSE_LL,
            _mouseCallback,
            hModule,
            0 // 0 = global hook (all threads)
        );

        // ── Initial correction ────────────────────────────────────────────────
        // The hooks above only fire on CHANGES.  If Qt repositioned the window
        // between ShowWindow in EmbedWindow and this Start() call, the window is
        // already stable at the wrong position and no hook fires to correct it.
        // Perform an immediate snap to guarantee the initial position is correct.
        if (Win32WindowNativeMethods.GetWindowRect(_container, out RECT startRect))
            RepositionEmbedded(startRect, sendWmSize: true);
    }

    public void Stop()
    {
        // ── Unhook low-level mouse hook ───────────────────────────────────────
        if (_mouseHook != nint.Zero)
        {
            Win32WindowNativeMethods.UnhookWindowsHookEx(_mouseHook);
            _mouseHook = nint.Zero;
        }

        _mouseCallback = null;

        // ── Unhook WinEvent hooks ─────────────────────────────────────────────
        UnhookWinEvent(ref _locationHook, ref _locationCallback);
        UnhookWinEvent(ref _visibilityHook, ref _visibilityCallback);
        UnhookWinEvent(ref _snapBackHook, ref _snapBackCallback);
        UnhookWinEvent(ref _destroyHook, ref _destroyCallback);
        UnhookWinEvent(ref _foregroundHook, ref _foregroundCallback);

        // ── Detach thread input ───────────────────────────────────────────────
        if (_inputAttached)
        {
            Win32WindowNativeMethods.AttachThreadInput(_uiThreadId, _pcsx2ThreadId, fAttach: false);
            _inputAttached = false;
        }

        _focusIsOnEmbedded = false;
        _embedded = nint.Zero;
        _container = nint.Zero;
        _rootWindow = nint.Zero;
    }

    // ── WinEvent callbacks (all run on the Avalonia UI thread) ────────────────

    private void OnContainerLocationChanged(
        nint hWinEventHook,
        uint @event,
        nint hwnd,
        int idObject,
        int idChild,
        uint idEventThread,
        uint dwmsEventTime
    )
    {
        // Accept both the container itself AND the Avalonia root window.
        // When only the root window moves (fullscreen ↔ windowed, display change),
        // the container's position relative to its parent is unchanged, so Windows
        // does NOT fire EVENT_OBJECT_LOCATIONCHANGE for the container.  But its
        // screen rect HAS changed, and the WS_POPUP embedded window uses screen
        // coordinates — so we must reposition it.
        if (idObject != Win32WindowNativeMethods.OBJID_SELF || (hwnd != _container && hwnd != _rootWindow))
            return;

        if (!Win32WindowNativeMethods.IsWindow(_container) || !Win32WindowNativeMethods.IsWindow(_embedded))
            return;

        if (!Win32WindowNativeMethods.IsWindowVisible(_container))
            return;

        if (!Win32WindowNativeMethods.GetWindowRect(_container, out RECT cr))
            return;

        // Update the cached rect for the mouse hook.
        _cachedContainerRect = cr;

        RepositionEmbedded(cr, sendWmSize: true);
    }

    private void OnEmbeddedLocationChanged(
        nint hWinEventHook,
        uint @event,
        nint hwnd,
        int idObject,
        int idChild,
        uint idEventThread,
        uint dwmsEventTime
    )
    {
        if (idObject != Win32WindowNativeMethods.OBJID_SELF || hwnd != _embedded)
            return;

        if (!Win32WindowNativeMethods.IsWindow(_container) || !Win32WindowNativeMethods.IsWindow(_embedded))
            return;

        if (!Win32WindowNativeMethods.IsWindowVisible(_container))
            return;

        if (!Win32WindowNativeMethods.GetWindowRect(_container, out RECT cr))
            return;

        // Snap-back only — do NOT send WM_SIZE here to avoid a feedback loop
        // (WM_SIZE → Qt resizes → LOCATIONCHANGE → snap-back → WM_SIZE → …).
        RepositionEmbedded(cr, sendWmSize: false);
    }

    private void RepositionEmbedded(RECT containerRect, bool sendWmSize)
    {
        if (!Win32WindowNativeMethods.GetWindowRect(_embedded, out RECT er))
            return;

        bool drifted =
            er.Left != containerRect.Left
            || er.Top != containerRect.Top
            || er.Width != containerRect.Width
            || er.Height != containerRect.Height;

        if (!drifted)
            return;

        // After SetParent, coordinates are parent-relative regardless of WS_POPUP.
        // Position at (0, 0) to fill the container from its top-left corner.
        Win32WindowNativeMethods.MoveWindow(_embedded, 0, 0, containerRect.Width, containerRect.Height, bRepaint: true);

        if (sendWmSize)
        {
            nint lParam = (nint)(((containerRect.Height & 0xFFFF) << 16) | (containerRect.Width & 0xFFFF));
            Win32WindowNativeMethods.PostMessage(
                _embedded,
                Win32WindowNativeMethods.WM_SIZE,
                Win32WindowNativeMethods.SIZE_RESTORED,
                lParam
            );
        }
    }

    private void OnVisibilityChanged(
        nint hWinEventHook,
        uint @event,
        nint hwnd,
        int idObject,
        int idChild,
        uint idEventThread,
        uint dwmsEventTime
    )
    {
        if (idObject != Win32WindowNativeMethods.OBJID_SELF || hwnd != _container)
            return;

        if (!Win32WindowNativeMethods.IsWindow(_embedded))
            return;

        if (@event == Win32WindowNativeMethods.EVENT_OBJECT_HIDE)
        {
            Win32WindowNativeMethods.ShowWindow(_embedded, Win32WindowNativeMethods.SW_HIDE);
        }
        else if (@event == Win32WindowNativeMethods.EVENT_OBJECT_SHOW)
        {
            if (Win32WindowNativeMethods.GetWindowRect(_container, out RECT cr))
            {
                _cachedContainerRect = cr;
                Win32WindowNativeMethods.MoveWindow(_embedded, 0, 0, cr.Width, cr.Height, bRepaint: true);
            }

            Win32WindowNativeMethods.ShowWindow(_embedded, Win32WindowNativeMethods.SW_SHOWNOACTIVATE);

            // ShowWindow may trigger Qt to reposition the embedded window
            // (e.g. WM_SHOWWINDOW → internal layout recalculation).
            // Re-read the container rect and snap-back immediately.
            if (Win32WindowNativeMethods.GetWindowRect(_container, out RECT cr2))
            {
                _cachedContainerRect = cr2;
                Win32WindowNativeMethods.MoveWindow(_embedded, 0, 0, cr2.Width, cr2.Height, bRepaint: true);

                nint lParam = (nint)(((cr2.Height & 0xFFFF) << 16) | (cr2.Width & 0xFFFF));
                Win32WindowNativeMethods.PostMessage(
                    _embedded,
                    Win32WindowNativeMethods.WM_SIZE,
                    Win32WindowNativeMethods.SIZE_RESTORED,
                    lParam
                );
            }
        }
    }

    private void OnEmbeddedDestroyed(
        nint hWinEventHook,
        uint @event,
        nint hwnd,
        int idObject,
        int idChild,
        uint idEventThread,
        uint dwmsEventTime
    )
    {
        if (idObject != Win32WindowNativeMethods.OBJID_SELF || hwnd != _embedded)
            return;

        // The embedded HWND is gone (PCSX2 recreated its DisplaySurface).
        // Stop all hooks and notify the subscriber to re-discover the new window.
        Stop();
        EmbeddedWindowLost?.Invoke(this, EventArgs.Empty);
    }

    private void OnForegroundChanged(
        nint hWinEventHook,
        uint @event,
        nint hwnd,
        int idObject,
        int idChild,
        uint idEventThread,
        uint dwmsEventTime
    )
    {
        // Only act when the Avalonia root window itself gains the foreground.
        // Other OT2 windows (e.g. dialogs) produce this event too; ignore them.
        if (hwnd != _rootWindow)
            return;

        if (!_inputAttached || _embedded == nint.Zero || _container == nint.Zero)
            return;

        // The OS already moved keyboard focus to Avalonia, so _focusIsOnEmbedded is now
        // stale regardless of its previous value.  Resetting it here ensures the mouse
        // hook's enter-transition fires correctly the next time the cursor enters the game
        // area — even if the cursor never left the area during the alt-tab away.
        _focusIsOnEmbedded = false;

        // If the cursor is already inside the game area (common when the user alt-tabbed
        // back and the mouse didn't move), restore focus to PCSX2 immediately so keyboard
        // input reaches the game without requiring a mouse wiggle.
        if (!Win32WindowNativeMethods.GetCursorPos(out POINT cursor))
            return;

        RECT cr = _cachedContainerRect;
        bool cursorInGame = cr.Left <= cursor.X && cursor.X < cr.Right && cr.Top <= cursor.Y && cursor.Y < cr.Bottom;

        if (cursorInGame && Win32WindowNativeMethods.IsWindowVisible(_container))
        {
            Win32WindowNativeMethods.SetFocus(_embedded);
            _focusIsOnEmbedded = true;
        }
    }

    // ── Low-level mouse hook callback ─────────────────────────────────────────
    //
    // WH_MOUSE_LL fires on every mouse input event system-wide.  The callback
    // runs on the thread that installed the hook (our UI thread).  The hot path
    // is a handful of integer comparisons against the cached container RECT —
    // SetFocus is only called on enter/leave transitions to keep the hook fast.

    private unsafe nint OnMouseEvent(int nCode, nint wParam, nint lParam)
    {
        if (
            nCode >= Win32WindowNativeMethods.HC_ACTION
            && (uint)wParam == Win32WindowNativeMethods.WM_MOUSEMOVE
            && _inputAttached
            && _embedded != nint.Zero
            && _container != nint.Zero
        )
        {
            // Zero-allocation read: cast the unmanaged pointer directly.
            POINT cursor = ((Win32WindowNativeMethods.MSLLHOOKSTRUCT*)lParam)->pt;

            // Don't interfere while Avalonia has mouse capture (e.g. Dock splitter drag).
            nint captured = Win32WindowNativeMethods.GetCapture();
            if (captured == nint.Zero)
            {
                RECT cr = _cachedContainerRect;
                bool cursorInGame =
                    cr.Left <= cursor.X && cursor.X < cr.Right && cr.Top <= cursor.Y && cursor.Y < cr.Bottom;

                if (cursorInGame && !_focusIsOnEmbedded)
                {
                    if (Win32WindowNativeMethods.IsWindowVisible(_container))
                    {
                        Win32WindowNativeMethods.SetFocus(_embedded);
                        _focusIsOnEmbedded = true;
                    }
                }
                else if (!cursorInGame && _focusIsOnEmbedded)
                {
                    _focusIsOnEmbedded = false;
                    nint avalonia = Win32WindowNativeMethods.GetAncestor(_container, Win32WindowNativeMethods.GA_ROOT);
                    if (avalonia != nint.Zero)
                        Win32WindowNativeMethods.SetFocus(avalonia);
                }
            }
        }

        return Win32WindowNativeMethods.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void UnhookWinEvent(ref nint hookHandle, ref Win32WindowNativeMethods.WinEventProc? callback)
    {
        if (hookHandle != nint.Zero)
        {
            Win32WindowNativeMethods.UnhookWinEvent(hookHandle);
            hookHandle = nint.Zero;
        }

        callback = null;
    }

    public void Dispose() => Stop();
}
