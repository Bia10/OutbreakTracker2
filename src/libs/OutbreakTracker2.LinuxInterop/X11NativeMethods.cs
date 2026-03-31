using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace OutbreakTracker2.LinuxInterop;

/// <summary>
/// P/Invoke declarations for libX11 (Xlib) used for cross-process window embedding on Linux.
/// Call <see cref="XInitThreads"/> once at application startup before any other X11 calls.
/// </summary>
[SupportedOSPlatform("linux")]
public static partial class X11NativeMethods
{
    /// <summary>
    /// Initialises Xlib for multi-threaded use. Must be the first Xlib call made.
    /// Returns non-zero on success. Idempotent — safe to call more than once.
    /// </summary>
    [LibraryImport("libX11.so.6", EntryPoint = "XInitThreads")]
    public static partial int XInitThreads();

    /// <summary>Opens a connection to the X server named by <paramref name="displayName"/>.
    /// Pass <see langword="null"/> to use the value of the <c>DISPLAY</c> environment variable.</summary>
    [LibraryImport("libX11.so.6", EntryPoint = "XOpenDisplay", StringMarshalling = StringMarshalling.Utf8)]
    public static partial nint XOpenDisplay(string? displayName);

    /// <summary>Closes a display connection previously opened with <see cref="XOpenDisplay"/>.</summary>
    [LibraryImport("libX11.so.6", EntryPoint = "XCloseDisplay")]
    public static partial int XCloseDisplay(nint display);

    /// <summary>Returns the root window of the default screen.</summary>
    [LibraryImport("libX11.so.6", EntryPoint = "XDefaultRootWindow")]
    public static partial nint XDefaultRootWindow(nint display);

    /// <summary>
    /// Creates a simple unmapped InputOutput child window.
    /// Pass <paramref name="border"/> = 0 and <paramref name="background"/> = 0 for no border / black background.
    /// </summary>
    [LibraryImport("libX11.so.6", EntryPoint = "XCreateSimpleWindow")]
    public static partial nint XCreateSimpleWindow(
        nint display,
        nint parent,
        int x,
        int y,
        uint width,
        uint height,
        uint borderWidth,
        nint border,
        nint background
    );

    /// <summary>Destroys the window and all of its sub-windows.</summary>
    [LibraryImport("libX11.so.6", EntryPoint = "XDestroyWindow")]
    public static partial int XDestroyWindow(nint display, nint w);

    /// <summary>Maps a window so it becomes visible.</summary>
    [LibraryImport("libX11.so.6", EntryPoint = "XMapWindow")]
    public static partial int XMapWindow(nint display, nint w);

    /// <summary>Unmaps a window (hides it without destroying it).</summary>
    [LibraryImport("libX11.so.6", EntryPoint = "XUnmapWindow")]
    public static partial int XUnmapWindow(nint display, nint w);

    /// <summary>Moves <paramref name="w"/> so that it becomes a child of <paramref name="parent"/>.</summary>
    [LibraryImport("libX11.so.6", EntryPoint = "XReparentWindow")]
    public static partial int XReparentWindow(nint display, nint w, nint parent, int x, int y);

    /// <summary>Changes the geometry (position and size) of a window atomically.</summary>
    [LibraryImport("libX11.so.6", EntryPoint = "XMoveResizeWindow")]
    public static partial int XMoveResizeWindow(nint display, nint w, int x, int y, uint width, uint height);

    /// <summary>Flushes the output buffer. Does not wait for a server reply.</summary>
    [LibraryImport("libX11.so.6", EntryPoint = "XFlush")]
    public static partial int XFlush(nint display);

    /// <summary>Flushes the output buffer and optionally waits for all events to be processed.</summary>
    [LibraryImport("libX11.so.6", EntryPoint = "XSync")]
    public static partial int XSync(nint display, [MarshalAs(UnmanagedType.Bool)] bool discard);

    /// <summary>
    /// Returns the parent, root, and children of a window.
    /// The caller must pass <paramref name="childrenReturn"/> to <see cref="XFree"/> when done.
    /// </summary>
    [LibraryImport("libX11.so.6", EntryPoint = "XQueryTree")]
    public static partial int XQueryTree(
        nint display,
        nint w,
        out nint rootReturn,
        out nint parentReturn,
        out nint childrenReturn,
        out uint nChildrenReturn
    );

    /// <summary>Frees memory allocated by Xlib (e.g. the children array from <see cref="XQueryTree"/>).</summary>
    [LibraryImport("libX11.so.6", EntryPoint = "XFree")]
    public static partial int XFree(nint data);

    /// <summary>Returns the atom identifier for the given name, optionally creating it if it does not exist.</summary>
    [LibraryImport("libX11.so.6", EntryPoint = "XInternAtom", StringMarshalling = StringMarshalling.Utf8)]
    public static partial nint XInternAtom(
        nint display,
        string atomName,
        [MarshalAs(UnmanagedType.Bool)] bool onlyIfExists
    );

    /// <summary>
    /// Reads a window property.
    /// The caller must pass <paramref name="propReturn"/> to <see cref="XFree"/> when done.
    /// Returns 0 (<c>Success</c>) on success.
    /// Pass <see langword="0"/> for <paramref name="reqType"/> to accept any type (AnyPropertyType).
    /// </summary>
    [LibraryImport("libX11.so.6", EntryPoint = "XGetWindowProperty")]
    public static partial int XGetWindowProperty(
        nint display,
        nint w,
        nint property,
        long longOffset,
        long longLength,
        [MarshalAs(UnmanagedType.Bool)] bool delete,
        nint reqType,
        out nint actualTypeReturn,
        out int actualFormatReturn,
        out ulong nItemsReturn,
        out ulong bytesAfterReturn,
        out nint propReturn
    );

    /// <summary>
    /// Sets the input focus to <paramref name="w"/>.
    /// Use <c>revertTo = 1</c> (RevertToParent) for embedded windows.
    /// </summary>
    [LibraryImport("libX11.so.6", EntryPoint = "XSetInputFocus")]
    public static partial int XSetInputFocus(nint display, nint w, int revertTo, nint time);
}
