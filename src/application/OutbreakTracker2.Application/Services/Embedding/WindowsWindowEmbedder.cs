using System.Runtime.Versioning;
using OutbreakTracker2.WinInterop;

namespace OutbreakTracker2.Application.Services.Embedding;

/// <summary>
/// Windows implementation of <see cref="IWindowEmbedder"/> using Win32 <c>user32.dll</c>.
/// Creates a <c>STATIC</c> class child window as a container, then calls <c>SetParent</c>
/// to embed the PCSX2 window and strips its decorations (caption, border, resize frame).
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsWindowEmbedder : IWindowEmbedder
{
    public bool IsSupported => true;

    // ── Container management ──────────────────────────────────────────────────

    public nint CreateContainerWindow(nint parentHandle, int width, int height)
    {
        // The "STATIC" window class is registered by Windows; it acts as a plain container.
        // WS_CHILD | WS_VISIBLE | WS_CLIPCHILDREN ensures it clips embedded child content.
        nint hwnd = Win32WindowNativeMethods.CreateWindowEx(
            0,
            "STATIC",
            lpWindowName: null,
            (uint)(
                Win32WindowNativeMethods.WS_CHILD
                | Win32WindowNativeMethods.WS_VISIBLE
                | Win32WindowNativeMethods.WS_CLIPCHILDREN
            ),
            0,
            0,
            Math.Max(1, width),
            Math.Max(1, height),
            parentHandle,
            nint.Zero,
            nint.Zero,
            nint.Zero
        );

        return hwnd;
    }

    public void DestroyContainerWindow(nint containerHandle)
    {
        if (containerHandle != nint.Zero)
            Win32WindowNativeMethods.DestroyWindow(containerHandle);
    }

    // ── Window discovery ──────────────────────────────────────────────────────

    /// <summary>
    /// Enumerates all visible top-level windows and returns the first whose owning PID matches
    /// <paramref name="pid"/>. Windows created by PCSX2-Qt may have multiple top-level HWNDs;
    /// we return the first visible one.
    /// </summary>
    public nint FindProcessWindow(int pid)
    {
        nint found = nint.Zero;

        Win32WindowNativeMethods.EnumWindows(
            (hwnd, _) =>
            {
                if (!Win32WindowNativeMethods.IsWindowVisible(hwnd))
                    return true;

                // Only consider true top-level windows (no owner/parent)
                if (Win32WindowNativeMethods.GetParent(hwnd) != nint.Zero)
                    return true;

                Win32WindowNativeMethods.GetWindowThreadProcessId(hwnd, out uint windowPid);
                if ((int)windowPid != pid)
                    return true;

                found = hwnd;
                return false; // Stop enumeration
            },
            nint.Zero
        );

        return found;
    }

    // ── Embedding ────────────────────────────────────────────────────────────

    public void EmbedWindow(nint containerHandle, nint targetHandle, int width, int height)
    {
        if (containerHandle == nint.Zero || targetHandle == nint.Zero)
            return;

        // Strip title bar and resize frame, add WS_CHILD
        long style = Win32WindowNativeMethods.GetWindowLongPtr(targetHandle, Win32WindowNativeMethods.GWL_STYLE);
        style &= ~(
            Win32WindowNativeMethods.WS_CAPTION
            | Win32WindowNativeMethods.WS_THICKFRAME
            | Win32WindowNativeMethods.WS_BORDER
            | Win32WindowNativeMethods.WS_SYSMENU
            | Win32WindowNativeMethods.WS_MINIMIZEBOX
            | Win32WindowNativeMethods.WS_MAXIMIZEBOX
        );
        style |= Win32WindowNativeMethods.WS_CHILD | Win32WindowNativeMethods.WS_CLIPCHILDREN;
        Win32WindowNativeMethods.SetWindowLongPtr(targetHandle, Win32WindowNativeMethods.GWL_STYLE, style);

        Win32WindowNativeMethods.SetParent(targetHandle, containerHandle);
        Win32WindowNativeMethods.MoveWindow(
            targetHandle,
            0,
            0,
            Math.Max(1, width),
            Math.Max(1, height),
            bRepaint: true
        );

        // Apply the style change and show the window
        Win32WindowNativeMethods.SetWindowPos(
            targetHandle,
            nint.Zero,
            0,
            0,
            Math.Max(1, width),
            Math.Max(1, height),
            Win32WindowNativeMethods.SWP_NOZORDER
                | Win32WindowNativeMethods.SWP_NOACTIVATE
                | Win32WindowNativeMethods.SWP_FRAMECHANGED
                | Win32WindowNativeMethods.SWP_SHOWWINDOW
        );
    }

    public void ResizeEmbeddedWindow(nint targetHandle, int width, int height)
    {
        if (targetHandle == nint.Zero)
            return;

        Win32WindowNativeMethods.MoveWindow(
            targetHandle,
            0,
            0,
            Math.Max(1, width),
            Math.Max(1, height),
            bRepaint: true
        );
    }

    public void ReleaseWindow(nint targetHandle)
    {
        if (targetHandle == nint.Zero)
            return;

        // Re-parent to desktop, restore a minimal set of decorations and show it
        Win32WindowNativeMethods.SetParent(targetHandle, nint.Zero);

        long style = Win32WindowNativeMethods.GetWindowLongPtr(targetHandle, Win32WindowNativeMethods.GWL_STYLE);
        style &= ~Win32WindowNativeMethods.WS_CHILD;
        style |= Win32WindowNativeMethods.WS_CAPTION | Win32WindowNativeMethods.WS_THICKFRAME;
        Win32WindowNativeMethods.SetWindowLongPtr(targetHandle, Win32WindowNativeMethods.GWL_STYLE, style);

        Win32WindowNativeMethods.SetWindowPos(
            targetHandle,
            nint.Zero,
            0,
            0,
            0,
            0,
            Win32WindowNativeMethods.SWP_NOZORDER
                | Win32WindowNativeMethods.SWP_NOACTIVATE
                | Win32WindowNativeMethods.SWP_FRAMECHANGED
                | Win32WindowNativeMethods.SWP_SHOWWINDOW
        );
    }

    // ── IDisposable ───────────────────────────────────────────────────────────

    public void Dispose() { }
}
