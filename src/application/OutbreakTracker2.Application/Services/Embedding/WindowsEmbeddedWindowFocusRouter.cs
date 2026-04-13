using System.Runtime.Versioning;
using OutbreakTracker2.WinInterop;

namespace OutbreakTracker2.Application.Services.Embedding;

[SupportedOSPlatform("windows")]
internal sealed class WindowsEmbeddedWindowFocusRouter : IDisposable
{
    private nint _foregroundHook;
    private Win32WindowNativeMethods.WinEventProc? _foregroundCallback;

    private nint _mouseHook;
    private Win32WindowNativeMethods.LowLevelMouseProc? _mouseCallback;

    private WindowsEmbeddedWindowSession? _session;
    private Func<RECT>? _getContainerRect;
    private bool _inputAttached;
    private bool _focusIsOnEmbedded;

    public void Start(WindowsEmbeddedWindowSession session, Func<RECT> getContainerRect)
    {
        Stop();

        ArgumentNullException.ThrowIfNull(getContainerRect);

        _session = session;
        _getContainerRect = getContainerRect;
        _inputAttached =
            session.Pcsx2ThreadId != 0
            && session.Pcsx2ThreadId != session.UiThreadId
            && Win32WindowNativeMethods.AttachThreadInput(session.UiThreadId, session.Pcsx2ThreadId, fAttach: true);

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

        _mouseCallback = OnMouseEvent;
        nint moduleHandle = Win32WindowNativeMethods.GetModuleHandle(null);
        _mouseHook = Win32WindowNativeMethods.SetWindowsHookEx(
            Win32WindowNativeMethods.WH_MOUSE_LL,
            _mouseCallback,
            moduleHandle,
            0
        );
    }

    public void Stop()
    {
        if (_mouseHook != nint.Zero)
        {
            Win32WindowNativeMethods.UnhookWindowsHookEx(_mouseHook);
            _mouseHook = nint.Zero;
        }

        _mouseCallback = null;
        UnhookWinEvent(ref _foregroundHook, ref _foregroundCallback);

        if (_inputAttached && _session is not null)
        {
            Win32WindowNativeMethods.AttachThreadInput(_session.UiThreadId, _session.Pcsx2ThreadId, fAttach: false);
            _inputAttached = false;
        }

        _focusIsOnEmbedded = false;
        _session = null;
        _getContainerRect = null;
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
        WindowsEmbeddedWindowSession? session = _session;
        Func<RECT>? getContainerRect = _getContainerRect;
        if (session is null || getContainerRect is null || hwnd != session.RootWindowHandle)
            return;

        if (!_inputAttached || session.EmbeddedHandle == nint.Zero || session.ContainerHandle == nint.Zero)
            return;

        _focusIsOnEmbedded = false;

        if (!Win32WindowNativeMethods.GetCursorPos(out POINT cursor))
            return;

        RECT containerRect = getContainerRect();
        bool cursorInGame =
            containerRect.Left <= cursor.X
            && cursor.X < containerRect.Right
            && containerRect.Top <= cursor.Y
            && cursor.Y < containerRect.Bottom;

        if (!cursorInGame || !Win32WindowNativeMethods.IsWindowVisible(session.ContainerHandle))
            return;

        Win32WindowNativeMethods.SetFocus(session.EmbeddedHandle);
        _focusIsOnEmbedded = true;
    }

    private unsafe nint OnMouseEvent(int nCode, nint wParam, nint lParam)
    {
        WindowsEmbeddedWindowSession? session = _session;
        Func<RECT>? getContainerRect = _getContainerRect;
        if (
            session is not null
            && getContainerRect is not null
            && nCode >= Win32WindowNativeMethods.HC_ACTION
            && (uint)wParam == Win32WindowNativeMethods.WM_MOUSEMOVE
            && _inputAttached
            && session.EmbeddedHandle != nint.Zero
            && session.ContainerHandle != nint.Zero
        )
        {
            POINT cursor = ((Win32WindowNativeMethods.MSLLHOOKSTRUCT*)lParam)->pt;
            nint captured = Win32WindowNativeMethods.GetCapture();

            if (captured == nint.Zero)
            {
                RECT containerRect = getContainerRect();
                bool cursorInGame =
                    containerRect.Left <= cursor.X
                    && cursor.X < containerRect.Right
                    && containerRect.Top <= cursor.Y
                    && cursor.Y < containerRect.Bottom;

                if (cursorInGame && !_focusIsOnEmbedded)
                {
                    if (Win32WindowNativeMethods.IsWindowVisible(session.ContainerHandle))
                    {
                        Win32WindowNativeMethods.SetFocus(session.EmbeddedHandle);
                        _focusIsOnEmbedded = true;
                    }
                }
                else if (!cursorInGame && _focusIsOnEmbedded)
                {
                    _focusIsOnEmbedded = false;

                    if (session.RootWindowHandle != nint.Zero)
                        Win32WindowNativeMethods.SetFocus(session.RootWindowHandle);
                }
            }
        }

        return Win32WindowNativeMethods.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

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
