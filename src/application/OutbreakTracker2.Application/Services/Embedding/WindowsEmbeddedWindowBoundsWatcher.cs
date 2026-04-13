using System.Runtime.Versioning;
using OutbreakTracker2.WinInterop;

namespace OutbreakTracker2.Application.Services.Embedding;

[SupportedOSPlatform("windows")]
internal sealed class WindowsEmbeddedWindowBoundsWatcher : IDisposable
{
    private nint _locationHook;
    private nint _visibilityHook;
    private nint _snapBackHook;

    private Win32WindowNativeMethods.WinEventProc? _locationCallback;
    private Win32WindowNativeMethods.WinEventProc? _visibilityCallback;
    private Win32WindowNativeMethods.WinEventProc? _snapBackCallback;

    private WindowsEmbeddedWindowSession? _session;
    private RECT _cachedContainerRect;

    public RECT CachedContainerRect => _cachedContainerRect;

    public void Start(WindowsEmbeddedWindowSession session)
    {
        Stop();

        _session = session;

        if (Win32WindowNativeMethods.GetWindowRect(session.ContainerHandle, out RECT initialRect))
            _cachedContainerRect = initialRect;

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

        _snapBackCallback = OnEmbeddedLocationChanged;
        _snapBackHook = Win32WindowNativeMethods.SetWinEventHook(
            Win32WindowNativeMethods.EVENT_OBJECT_LOCATIONCHANGE,
            Win32WindowNativeMethods.EVENT_OBJECT_LOCATIONCHANGE,
            nint.Zero,
            _snapBackCallback,
            session.Pcsx2ProcessId,
            0,
            Win32WindowNativeMethods.WINEVENT_OUTOFCONTEXT
        );

        if (Win32WindowNativeMethods.GetWindowRect(session.ContainerHandle, out RECT startRect))
            RepositionEmbedded(session, startRect, sendWmSize: true);
    }

    public void Stop()
    {
        UnhookWinEvent(ref _locationHook, ref _locationCallback);
        UnhookWinEvent(ref _visibilityHook, ref _visibilityCallback);
        UnhookWinEvent(ref _snapBackHook, ref _snapBackCallback);

        _session = null;
        _cachedContainerRect = default;
    }

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
        WindowsEmbeddedWindowSession? session = _session;
        if (
            session is null
            || idObject != Win32WindowNativeMethods.OBJID_SELF
            || (hwnd != session.ContainerHandle && hwnd != session.RootWindowHandle)
        )
        {
            return;
        }

        if (
            !Win32WindowNativeMethods.IsWindow(session.ContainerHandle)
            || !Win32WindowNativeMethods.IsWindow(session.EmbeddedHandle)
            || !Win32WindowNativeMethods.IsWindowVisible(session.ContainerHandle)
            || !Win32WindowNativeMethods.GetWindowRect(session.ContainerHandle, out RECT containerRect)
        )
        {
            return;
        }

        _cachedContainerRect = containerRect;
        RepositionEmbedded(session, containerRect, sendWmSize: true);
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
        WindowsEmbeddedWindowSession? session = _session;
        if (session is null || idObject != Win32WindowNativeMethods.OBJID_SELF || hwnd != session.EmbeddedHandle)
        {
            return;
        }

        if (
            !Win32WindowNativeMethods.IsWindow(session.ContainerHandle)
            || !Win32WindowNativeMethods.IsWindow(session.EmbeddedHandle)
            || !Win32WindowNativeMethods.IsWindowVisible(session.ContainerHandle)
            || !Win32WindowNativeMethods.GetWindowRect(session.ContainerHandle, out RECT containerRect)
        )
        {
            return;
        }

        RepositionEmbedded(session, containerRect, sendWmSize: false);
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
        WindowsEmbeddedWindowSession? session = _session;
        if (
            session is null
            || idObject != Win32WindowNativeMethods.OBJID_SELF
            || hwnd != session.ContainerHandle
            || !Win32WindowNativeMethods.IsWindow(session.EmbeddedHandle)
        )
        {
            return;
        }

        if (@event == Win32WindowNativeMethods.EVENT_OBJECT_HIDE)
        {
            Win32WindowNativeMethods.ShowWindow(session.EmbeddedHandle, Win32WindowNativeMethods.SW_HIDE);
            return;
        }

        if (@event != Win32WindowNativeMethods.EVENT_OBJECT_SHOW)
            return;

        if (Win32WindowNativeMethods.GetWindowRect(session.ContainerHandle, out RECT containerRect))
        {
            _cachedContainerRect = containerRect;
            Win32WindowNativeMethods.MoveWindow(
                session.EmbeddedHandle,
                0,
                0,
                containerRect.Width,
                containerRect.Height,
                bRepaint: true
            );
        }

        Win32WindowNativeMethods.ShowWindow(session.EmbeddedHandle, Win32WindowNativeMethods.SW_SHOWNOACTIVATE);

        if (!Win32WindowNativeMethods.GetWindowRect(session.ContainerHandle, out RECT postShowRect))
            return;

        _cachedContainerRect = postShowRect;
        Win32WindowNativeMethods.MoveWindow(
            session.EmbeddedHandle,
            0,
            0,
            postShowRect.Width,
            postShowRect.Height,
            bRepaint: true
        );

        nint lParam = (nint)(((postShowRect.Height & 0xFFFF) << 16) | (postShowRect.Width & 0xFFFF));
        Win32WindowNativeMethods.PostMessage(
            session.EmbeddedHandle,
            Win32WindowNativeMethods.WM_SIZE,
            Win32WindowNativeMethods.SIZE_RESTORED,
            lParam
        );
    }

    private static void RepositionEmbedded(WindowsEmbeddedWindowSession session, RECT containerRect, bool sendWmSize)
    {
        if (!Win32WindowNativeMethods.GetWindowRect(session.EmbeddedHandle, out RECT embeddedRect))
            return;

        bool drifted =
            embeddedRect.Left != containerRect.Left
            || embeddedRect.Top != containerRect.Top
            || embeddedRect.Width != containerRect.Width
            || embeddedRect.Height != containerRect.Height;

        if (!drifted)
            return;

        Win32WindowNativeMethods.MoveWindow(
            session.EmbeddedHandle,
            0,
            0,
            containerRect.Width,
            containerRect.Height,
            bRepaint: true
        );

        if (!sendWmSize)
            return;

        nint lParam = (nint)(((containerRect.Height & 0xFFFF) << 16) | (containerRect.Width & 0xFFFF));
        Win32WindowNativeMethods.PostMessage(
            session.EmbeddedHandle,
            Win32WindowNativeMethods.WM_SIZE,
            Win32WindowNativeMethods.SIZE_RESTORED,
            lParam
        );
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
