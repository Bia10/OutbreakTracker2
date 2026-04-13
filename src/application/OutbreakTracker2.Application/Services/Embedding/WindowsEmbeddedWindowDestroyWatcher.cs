using System.Runtime.Versioning;
using OutbreakTracker2.WinInterop;

namespace OutbreakTracker2.Application.Services.Embedding;

[SupportedOSPlatform("windows")]
internal sealed class WindowsEmbeddedWindowDestroyWatcher : IDisposable
{
    public event EventHandler? EmbeddedWindowDestroyed;

    private nint _destroyHook;
    private Win32WindowNativeMethods.WinEventProc? _destroyCallback;
    private WindowsEmbeddedWindowSession? _session;

    public void Start(WindowsEmbeddedWindowSession session)
    {
        Stop();

        _session = session;
        _destroyCallback = OnEmbeddedDestroyed;
        _destroyHook = Win32WindowNativeMethods.SetWinEventHook(
            Win32WindowNativeMethods.EVENT_OBJECT_DESTROY,
            Win32WindowNativeMethods.EVENT_OBJECT_DESTROY,
            nint.Zero,
            _destroyCallback,
            session.Pcsx2ProcessId,
            0,
            Win32WindowNativeMethods.WINEVENT_OUTOFCONTEXT
        );
    }

    public void Stop()
    {
        UnhookWinEvent(ref _destroyHook, ref _destroyCallback);
        _session = null;
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
        WindowsEmbeddedWindowSession? session = _session;
        if (session is null || idObject != Win32WindowNativeMethods.OBJID_SELF || hwnd != session.EmbeddedHandle)
        {
            return;
        }

        EmbeddedWindowDestroyed?.Invoke(this, EventArgs.Empty);
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
