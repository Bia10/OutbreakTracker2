using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace OutbreakTracker2.WinInterop;

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

    // Common window styles
    public const long WS_CHILD = 0x40000000L;
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
    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint SWP_FRAMECHANGED = 0x0020;
    public const uint SWP_SHOWWINDOW = 0x0040;

    // ShowWindow commands
    public const int SW_HIDE = 0;
    public const int SW_SHOW = 5;

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
}
