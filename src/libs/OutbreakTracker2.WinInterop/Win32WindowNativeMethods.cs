using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace OutbreakTracker2.WinInterop;

/// <summary>Screen coordinate point, as returned by <see cref="Win32WindowNativeMethods.GetCursorPos"/>.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct POINT
{
    public int X;
    public int Y;
}

/// <summary>Bounding rectangle in screen coordinates, as returned by <see cref="Win32WindowNativeMethods.GetWindowRect"/>.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public readonly int Width => Right - Left;
    public readonly int Height => Bottom - Top;
    public readonly int Area => Width * Height;
}

/// <summary>
/// P/Invoke declarations for user32.dll window management APIs.
/// Used for embedding a child process's window (e.g. PCSX2) into a Win32 host window.
/// </summary>
[SupportedOSPlatform("windows")]
public static partial class Win32WindowNativeMethods
{
    // Window style index values for GetWindowLongPtr / SetWindowLongPtr
    public const int GWL_STYLE = -16;
    public const int GWL_EXSTYLE = -20;

    // Extended window styles
    public const long WS_EX_NOACTIVATE = 0x08000000L;

    // Common window styles
    public const long WS_CHILD = 0x40000000L;
    public const long WS_POPUP = 0x80000000L;
    public const long WS_VISIBLE = 0x10000000L;
    public const long WS_CAPTION = 0x00C00000L;
    public const long WS_THICKFRAME = 0x00040000L;
    public const long WS_BORDER = 0x00800000L;
    public const long WS_SYSMENU = 0x00080000L;
    public const long WS_MINIMIZEBOX = 0x00020000L;
    public const long WS_MAXIMIZEBOX = 0x00010000L;
    public const long WS_CLIPCHILDREN = 0x02000000L;
    public const long WS_CLIPSIBLINGS = 0x04000000L;

    // SetWindowPos flags
    public const uint SWP_NOZORDER = 0x0004;
    public const uint SWP_NOMOVE = 0x0002;
    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint SWP_FRAMECHANGED = 0x0020;
    public const uint SWP_SHOWWINDOW = 0x0040;

    // ShowWindow commands
    public const int SW_HIDE = 0;
    public const int SW_MINIMIZE = 6;
    public const int SW_RESTORE = 9;
    public const int SW_SHOW = 5;
    public const int SW_SHOWNOACTIVATE = 8;

    // WM_SIZE constants
    public const uint WM_SIZE = 0x0005;
    public const nint SIZE_RESTORED = 0;

    /// <summary>
    /// Unmanaged callback used by <see cref="EnumWindows"/>.
    /// Return <see langword="true"/> to continue enumeration, <see langword="false"/> to stop.
    /// </summary>
    public delegate bool EnumWindowsProc(nint hWnd, nint lParam);

    /// <summary>Changes the parent window of the specified child window.</summary>
    [LibraryImport("user32.dll", EntryPoint = "SetParent")]
    public static partial nint SetParent(nint hWndChild, nint hWndNewParent);

    /// <summary>Returns the extended style or style bits of the specified window.</summary>
    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    public static partial long GetWindowLongPtr(nint hWnd, int nIndex);

    /// <summary>Sets the style bits of the specified window and triggers a <c>WM_STYLECHANGED</c> message.</summary>
    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    public static partial long SetWindowLongPtr(nint hWnd, int nIndex, long dwNewLong);

    /// <summary>Moves and resizes a window.</summary>
    [LibraryImport("user32.dll", EntryPoint = "MoveWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool MoveWindow(
        nint hWnd,
        int x,
        int y,
        int nWidth,
        int nHeight,
        [MarshalAs(UnmanagedType.Bool)] bool bRepaint
    );

    /// <summary>Changes the size, position, and Z-order of a window.</summary>
    [LibraryImport("user32.dll", EntryPoint = "SetWindowPos")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    /// <summary>Shows or hides a window.</summary>
    [LibraryImport("user32.dll", EntryPoint = "ShowWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ShowWindow(nint hWnd, int nCmdShow);

    /// <summary>Returns whether the specified window is visible.</summary>
    [LibraryImport("user32.dll", EntryPoint = "IsWindowVisible")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsWindowVisible(nint hWnd);

    /// <summary>Returns the thread and optionally the PID that created the specified window.</summary>
    [LibraryImport("user32.dll", EntryPoint = "GetWindowThreadProcessId")]
    public static partial uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

    /// <summary>Enumerates all top-level windows, calling <paramref name="lpEnumFunc"/> for each.</summary>
    [LibraryImport("user32.dll", EntryPoint = "EnumWindows")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

    /// <summary>Creates a window with an extended style.</summary>
    [LibraryImport("user32.dll", EntryPoint = "CreateWindowExW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint CreateWindowEx(
        uint dwExStyle,
        string lpClassName,
        string? lpWindowName,
        uint dwStyle,
        int x,
        int y,
        int nWidth,
        int nHeight,
        nint hWndParent,
        nint hMenu,
        nint hInstance,
        nint lpParam
    );

    /// <summary>Destroys the specified window.</summary>
    [LibraryImport("user32.dll", EntryPoint = "DestroyWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DestroyWindow(nint hWnd);

    /// <summary>Returns the parent or owner of the specified window, or zero if it is a top-level window.</summary>
    [LibraryImport("user32.dll", EntryPoint = "GetParent")]
    public static partial nint GetParent(nint hWnd);

    /// <summary>Returns whether the specified window handle identifies an existing window.</summary>
    [LibraryImport("user32.dll", EntryPoint = "IsWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsWindow(nint hWnd);

    /// <summary>
    /// Retrieves the dimensions of the bounding rectangle of the specified window in screen coordinates.
    /// </summary>
    [LibraryImport("user32.dll", EntryPoint = "GetWindowRect")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetWindowRect(nint hWnd, out RECT lpRect);

    /// <summary>
    /// Retrieves the name of the class to which the specified window belongs.
    /// Returns the number of characters copied (excluding the null terminator), or 0 on failure.
    /// </summary>
    [DllImport("user32.dll", EntryPoint = "GetClassNameW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern int GetClassName(nint hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

    /// <summary>
    /// Copies the text of the specified window's title bar into a buffer.
    /// Returns the number of characters copied (excluding the null terminator), or 0 on failure.
    /// </summary>
    [DllImport("user32.dll", EntryPoint = "GetWindowTextW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern int GetWindowText(nint hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    /// <summary>
    /// Adds an invalidation region to the window so the next <c>WM_PAINT</c> covers it.
    /// Pass <see cref="nint.Zero"/> for <paramref name="lpRect"/> to invalidate the whole client area.
    /// </summary>
    [LibraryImport("user32.dll", EntryPoint = "InvalidateRect")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool InvalidateRect(nint hWnd, nint lpRect, [MarshalAs(UnmanagedType.Bool)] bool bErase);

    /// <summary>
    /// Sends a message to the window's message queue and returns immediately (non-blocking).
    /// </summary>
    [LibraryImport("user32.dll", EntryPoint = "PostMessageW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool PostMessage(nint hWnd, uint msg, nint wParam, nint lParam);

    /// <summary>Enumerates the child windows of <paramref name="hWndParent"/>.</summary>
    [LibraryImport("user32.dll", EntryPoint = "EnumChildWindows")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool EnumChildWindows(nint hWndParent, EnumWindowsProc lpEnumFunc, nint lParam);

    /// <summary>
    /// Updates the client area of the specified window by sending a <c>WM_PAINT</c> message
    /// synchronously if the window's update region is not empty.
    /// </summary>
    [LibraryImport("user32.dll", EntryPoint = "UpdateWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool UpdateWindow(nint hWnd);

    /// <summary>
    /// Sets the keyboard focus to the specified window.  The window must be attached to the
    /// calling thread's message queue, or the calling thread must be attached to the target
    /// thread's input state via <see cref="AttachThreadInput"/>.
    /// </summary>
    [LibraryImport("user32.dll", EntryPoint = "SetFocus")]
    public static partial nint SetFocus(nint hWnd);

    /// <summary>
    /// Returns the handle of the window that has the keyboard focus within the calling
    /// thread's message queue (also reflects the shared state when thread inputs are attached).
    /// </summary>
    [LibraryImport("user32.dll", EntryPoint = "GetFocus")]
    public static partial nint GetFocus();

    /// <summary>
    /// Attaches or detaches the input processing mechanism of one thread to that of another,
    /// enabling cross-thread <see cref="SetFocus"/> calls.
    /// </summary>
    [LibraryImport("user32.dll", EntryPoint = "AttachThreadInput")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AttachThreadInput(
        uint idAttach,
        uint idAttachTo,
        [MarshalAs(UnmanagedType.Bool)] bool fAttach
    );

    /// <summary>Returns the position of the cursor in screen coordinates.</summary>
    [LibraryImport("user32.dll", EntryPoint = "GetCursorPos")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetCursorPos(out POINT lpPoint);

    /// <summary>Returns the thread identifier of the calling thread (Win32 thread ID).</summary>
    [LibraryImport("kernel32.dll", EntryPoint = "GetCurrentThreadId")]
    public static partial uint GetCurrentThreadId();

    // ── WinEvent hook ─────────────────────────────────────────────────────────

    // Events
    public const uint EVENT_OBJECT_DESTROY = 0x8001;
    public const uint EVENT_OBJECT_SHOW = 0x8002;
    public const uint EVENT_OBJECT_HIDE = 0x8003;
    public const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;

    // SetWinEventHook flags
    /// <summary>
    /// The callback is posted to the message queue of the thread that called
    /// <see cref="SetWinEventHook"/>.  That thread must pump its Win32 message queue
    /// (Avalonia's UI thread does this automatically).
    /// </summary>
    public const uint WINEVENT_OUTOFCONTEXT = 0x0000;

    /// <summary>
    /// <c>idObject</c> value passed to a <see cref="WinEventProc"/> callback when the event
    /// applies to the window itself rather than one of its child objects (e.g. scroll bars).
    /// </summary>
    public const int OBJID_SELF = 0;

    /// <summary>
    /// Callback signature for <see cref="SetWinEventHook"/>.
    /// Must be stored in a field to prevent GC collection while the hook is active.
    /// </summary>
    public delegate void WinEventProc(
        nint hWinEventHook,
        uint @event,
        nint hwnd,
        int idObject,
        int idChild,
        uint idEventThread,
        uint dwmsEventTime
    );

    /// <summary>
    /// Installs a WinEvent hook that receives notifications when the specified events fire.
    /// Must be unhooked with <see cref="UnhookWinEvent"/> from the <em>same thread</em>.
    /// </summary>
    /// <remarks>
    /// With <see cref="WINEVENT_OUTOFCONTEXT"/> the callback is posted to the calling
    /// thread's queue and delivered the next time it pumps messages — no in-process injection
    /// is required and the target process does not need to cooperate.
    /// </remarks>
    [DllImport("user32.dll", EntryPoint = "SetWinEventHook")]
    public static extern nint SetWinEventHook(
        uint eventMin,
        uint eventMax,
        nint hmodWinEventProc,
        WinEventProc lpfnWinEventProc,
        uint idProcess,
        uint idThread,
        uint dwFlags
    );

    /// <summary>Removes a WinEvent hook previously installed with <see cref="SetWinEventHook"/>.</summary>
    [LibraryImport("user32.dll", EntryPoint = "UnhookWinEvent")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool UnhookWinEvent(nint hWinEventHook);

    // ── Low-level mouse hook ──────────────────────────────────────────────────

    /// <summary>Hook type for <see cref="SetWindowsHookEx"/>: low-level mouse input events.</summary>
    public const int WH_MOUSE_LL = 14;

    /// <summary>The hook procedure must process the message.</summary>
    public const int HC_ACTION = 0;

    /// <summary>Window message: mouse moved.</summary>
    public const uint WM_MOUSEMOVE = 0x0200;

    /// <summary>Data structure passed via <c>lParam</c> to a <see cref="LowLevelMouseProc"/> callback.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public nuint dwExtraInfo;
    }

    /// <summary>
    /// Callback signature for a low-level mouse hook installed via <see cref="SetWindowsHookEx"/>.
    /// Must be stored in a field to prevent GC collection while the hook is active.
    /// </summary>
    public delegate nint LowLevelMouseProc(int nCode, nint wParam, nint lParam);

    /// <summary>Installs a low-level mouse hook (or other hook type).</summary>
    [DllImport("user32.dll", EntryPoint = "SetWindowsHookExW")]
    public static extern nint SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, nint hMod, uint dwThreadId);

    /// <summary>Removes a hook installed with <see cref="SetWindowsHookEx"/>.</summary>
    [LibraryImport("user32.dll", EntryPoint = "UnhookWindowsHookEx")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool UnhookWindowsHookEx(nint hhk);

    /// <summary>Passes the hook information to the next hook procedure in the current hook chain.</summary>
    [DllImport("user32.dll", EntryPoint = "CallNextHookEx")]
    public static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    /// <summary>Retrieves a module handle for the specified module.</summary>
    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint GetModuleHandle(string? lpModuleName);

    // ── Additional window queries ─────────────────────────────────────────────

    /// <summary>Flag for <see cref="GetAncestor"/>: retrieves the parent window (as set by <c>SetParent</c>).</summary>
    public const uint GA_PARENT = 1;

    /// <summary>Flag for <see cref="GetAncestor"/>: retrieves the root (top-level) window.</summary>
    public const uint GA_ROOT = 2;

    /// <summary>Retrieves the ancestor window of the specified window at the given level.</summary>
    [LibraryImport("user32.dll", EntryPoint = "GetAncestor")]
    public static partial nint GetAncestor(nint hwnd, uint gaFlags);

    /// <summary>
    /// Retrieves the handle of the window (if any) that has captured the mouse within the
    /// calling thread's message queue.  Returns <see cref="nint.Zero"/> if no capture is active.
    /// </summary>
    [LibraryImport("user32.dll", EntryPoint = "GetCapture")]
    public static partial nint GetCapture();

    /// <summary>Returns the foreground window handle.</summary>
    [LibraryImport("user32.dll", EntryPoint = "GetForegroundWindow")]
    public static partial nint GetForegroundWindow();
}
