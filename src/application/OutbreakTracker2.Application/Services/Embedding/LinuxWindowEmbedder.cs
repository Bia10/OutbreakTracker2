#if LINUX
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using OutbreakTracker2.LinuxInterop;

namespace OutbreakTracker2.Application.Services.Embedding;

/// <summary>
/// Linux/X11 implementation of <see cref="IWindowEmbedder"/> using libX11.
/// Opens a dedicated <c>Display</c> connection so all X11 calls are independent of Avalonia's
/// internal display. Calls <see cref="X11NativeMethods.XInitThreads"/> to allow safe use from
/// background threads (window discovery polls run on the thread-pool).
/// </summary>
/// <remarks>
/// <para><b>WSLg / WSL2 testing notes:</b></para>
/// <list type="bullet">
///   <item><description>
///     WSLg sets <c>DISPLAY=:0</c> automatically. The app will connect to XWayland via
///     <see cref="X11NativeMethods.XOpenDisplay"/> with <see langword="null"/> (reads
///     <c>$DISPLAY</c>). No manual configuration is required.
///   </description></item>
///   <item><description>
///     Avalonia uses the X11 backend under WSLg. Ensure the app is not forced onto Wayland
///     (do not set <c>WAYLAND_DISPLAY</c> without also setting <c>DISPLAY</c>).
///   </description></item>
///   <item><description>
///     PCSX2 <b>must</b> be launched via OutbreakTracker2 (not started externally).
///     This satisfies both the <c>process_vm_readv</c> child-process requirement
///     (<c>ptrace_scope=1</c>) <b>and</b> the <c>_NET_WM_PID</c> window-discovery match used by
///     <see cref="FindProcessWindow"/>.
///   </description></item>
///   <item><description>
///     If the PCSX2 window does not embed, run these diagnostic commands inside WSL:
///     <code>
///     echo $DISPLAY
///     xdpyinfo | head -5
///     xprop -id $(xdotool getactivewindow) _NET_WM_PID
///     </code>
///   </description></item>
/// </list>
/// </remarks>
[SupportedOSPlatform("linux")]
internal sealed class LinuxWindowEmbedder : IWindowEmbedder
{
    private nint _display;

    public LinuxWindowEmbedder()
    {
        // Must be the first Xlib call in the process to enable multi-thread safety.
        X11NativeMethods.XInitThreads();
        _display = X11NativeMethods.XOpenDisplay(null);
    }

    public bool IsSupported => _display != nint.Zero;

    // ── Container management ──────────────────────────────────────────────────

    public nint CreateContainerWindow(nint parentHandle, int width, int height)
    {
        nint win = X11NativeMethods.XCreateSimpleWindow(
            _display,
            parentHandle,
            0,
            0,
            (uint)Math.Max(1, width),
            (uint)Math.Max(1, height),
            0,
            0,
            0
        );

        X11NativeMethods.XMapWindow(_display, win);
        X11NativeMethods.XFlush(_display);
        return win;
    }

    public void DestroyContainerWindow(nint containerHandle)
    {
        if (containerHandle == nint.Zero)
            return;

        X11NativeMethods.XDestroyWindow(_display, containerHandle);
        X11NativeMethods.XFlush(_display);
    }

    // ── Window discovery ──────────────────────────────────────────────────────

    /// <summary>
    /// Searches direct children of the root window for a window whose <c>_NET_WM_PID</c>
    /// property matches <paramref name="pid"/>. Falls back to <c>WM_CLASS</c> name matching
    /// ("pcsx2") if the property is absent.
    /// Safe to call from any thread after <see cref="X11NativeMethods.XInitThreads"/>.
    /// </summary>
    public nint FindProcessWindow(int pid)
    {
        if (_display == nint.Zero)
            return nint.Zero;

        nint root = X11NativeMethods.XDefaultRootWindow(_display);

        int status = X11NativeMethods.XQueryTree(
            _display,
            root,
            out _,
            out _,
            out nint childrenPtr,
            out uint nChildren
        );

        if (status == 0 || childrenPtr == nint.Zero || nChildren == 0)
            return nint.Zero;

        try
        {
            nint pidAtom = X11NativeMethods.XInternAtom(_display, "_NET_WM_PID", onlyIfExists: false);
            nint wmClassAtom = X11NativeMethods.XInternAtom(_display, "WM_CLASS", onlyIfExists: false);

            for (uint i = 0; i < nChildren; i++)
            {
                nint child = Marshal.ReadIntPtr(childrenPtr + (int)(i * (uint)nint.Size));

                // Primary: match via _NET_WM_PID
                if (GetWindowPid(_display, child, pidAtom) == pid)
                    return child;

                // Fallback: match by WM_CLASS instance name "pcsx2" (covers Flatpak, Snap)
                if (MatchesWmClass(_display, child, wmClassAtom, "pcsx2"))
                    return child;
            }

            return nint.Zero;
        }
        finally
        {
            X11NativeMethods.XFree(childrenPtr);
        }
    }

    // ── Embedding ────────────────────────────────────────────────────────────

    public void EmbedWindow(nint containerHandle, nint targetHandle, int width, int height)
    {
        if (containerHandle == nint.Zero || targetHandle == nint.Zero)
            return;

        // Unmap so the reparent doesn't flash
        X11NativeMethods.XUnmapWindow(_display, targetHandle);

        X11NativeMethods.XReparentWindow(_display, targetHandle, containerHandle, 0, 0);
        X11NativeMethods.XMoveResizeWindow(
            _display,
            targetHandle,
            0,
            0,
            (uint)Math.Max(1, width),
            (uint)Math.Max(1, height)
        );

        X11NativeMethods.XMapWindow(_display, targetHandle);

        // XSync ensures the server has processed all requests before we return,
        // so Avalonia sees the new window geometry immediately.
        X11NativeMethods.XSync(_display, discard: false);
    }

    public void ResizeEmbeddedWindow(nint targetHandle, nint containerHandle, int width, int height)
    {
        if (targetHandle == nint.Zero)
            return;

        X11NativeMethods.XMoveResizeWindow(
            _display,
            targetHandle,
            0,
            0,
            (uint)Math.Max(1, width),
            (uint)Math.Max(1, height)
        );
        X11NativeMethods.XFlush(_display);
    }

    public void ReleaseWindow(nint targetHandle)
    {
        if (targetHandle == nint.Zero)
            return;

        nint root = X11NativeMethods.XDefaultRootWindow(_display);
        X11NativeMethods.XReparentWindow(_display, targetHandle, root, 0, 0);
        X11NativeMethods.XMapWindow(_display, targetHandle);
        X11NativeMethods.XSync(_display, discard: false);
    }

    public string GetDiagnosticInfo(int pid) => string.Empty;

    // ── X11 helpers ───────────────────────────────────────────────────────────

    private static int GetWindowPid(nint display, nint window, nint pidAtom)
    {
        int result = X11NativeMethods.XGetWindowProperty(
            display,
            window,
            pidAtom,
            0,
            1,
            delete: false,
            reqType: 0,
            out _,
            out int actualFormat,
            out ulong nItems,
            out _,
            out nint propPtr
        );

        if (result != 0 || propPtr == nint.Zero || nItems < 1 || actualFormat != 32)
            return -1;

        try
        {
            return Marshal.ReadInt32(propPtr);
        }
        finally
        {
            X11NativeMethods.XFree(propPtr);
        }
    }

    private static bool MatchesWmClass(nint display, nint window, nint wmClassAtom, string instanceName)
    {
        // WM_CLASS is two consecutive null-terminated strings: "instanceName\0className\0"
        int result = X11NativeMethods.XGetWindowProperty(
            display,
            window,
            wmClassAtom,
            0,
            256,
            delete: false,
            reqType: 0,
            out _,
            out int actualFormat,
            out ulong nItems,
            out _,
            out nint propPtr
        );

        if (result != 0 || propPtr == nint.Zero || nItems < 1 || actualFormat != 8)
            return false;

        try
        {
            // Read the first null-terminated string (instance name)
            string? value = Marshal.PtrToStringAnsi(propPtr);
            return string.Equals(value, instanceName, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            X11NativeMethods.XFree(propPtr);
        }
    }

    // ── IDisposable ───────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_display == nint.Zero)
            return;

        X11NativeMethods.XCloseDisplay(_display);
        _display = nint.Zero;
    }
}
#endif
