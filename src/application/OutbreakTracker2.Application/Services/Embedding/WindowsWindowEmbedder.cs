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
    /// Enumerates all visible top-level windows belonging to <paramref name="pid"/> and returns the
    /// one with the largest screen area.  This handles both PCSX2-Qt "integrated" mode (rendering
    /// inside the <c>QMainWindow</c>) and "separate rendering window" mode, where the game display
    /// is an owned popup window — effectively invisible to the old <c>GetParent</c> filter.
    /// </summary>
    /// <remarks>
    /// <c>EnumWindows</c> already iterates only true top-level windows (those without a Win32
    /// parent).  However a popup window with an <em>owner</em> is still a top-level window, and
    /// <c>GetParent</c> returns the owner handle for such windows — so using <c>GetParent != 0</c>
    /// as a filter incorrectly discards owned popup windows (the PCSX2 render surface).
    /// </remarks>
    public nint FindProcessWindow(int pid)
    {
        nint best = nint.Zero;
        int bestArea = 0;

        Win32WindowNativeMethods.EnumWindows(
            (hwnd, _) =>
            {
                if (!Win32WindowNativeMethods.IsWindowVisible(hwnd))
                    return true;

                Win32WindowNativeMethods.GetWindowThreadProcessId(hwnd, out uint windowPid);
                if ((int)windowPid != pid)
                    return true;

                if (!Win32WindowNativeMethods.GetWindowRect(hwnd, out RECT rect))
                    return true;

                int area = rect.Area;
                if (area > bestArea)
                {
                    bestArea = area;
                    best = hwnd;
                }

                return true; // Always continue — pick the largest, not the first
            },
            nint.Zero
        );

        return best;
    }

    // ── Embedding ────────────────────────────────────────────────────────────

    public void EmbedWindow(nint containerHandle, nint targetHandle, int width, int height)
    {
        if (containerHandle == nint.Zero || targetHandle == nint.Zero)
            return;

        // Strip decorations but KEEP WS_POPUP.
        //
        // Converting WS_POPUP → WS_CHILD via SetWindowLongPtr causes Qt-based applications
        // (PCSX2) to receive a WM_STYLECHANGED notification that their message loop interprets
        // as a top-level → child demotion; they terminate within ~1 second.  Retaining
        // WS_POPUP avoids that signal while still allowing SetParent to bind the window to
        // the container.  Qt's swap-chain rendering is tied to the HWND, not the style bits,
        // so the game continues to render normally.
        //
        // Consequence: WS_POPUP windows use SCREEN coordinates in MoveWindow / SetWindowPos,
        // not coordinates relative to the parent.  We read the container's screen rect and use
        // that as the position.
        long style = Win32WindowNativeMethods.GetWindowLongPtr(targetHandle, Win32WindowNativeMethods.GWL_STYLE);
        style &= ~(
            Win32WindowNativeMethods.WS_CAPTION
            | Win32WindowNativeMethods.WS_THICKFRAME
            | Win32WindowNativeMethods.WS_BORDER
            | Win32WindowNativeMethods.WS_SYSMENU
            | Win32WindowNativeMethods.WS_MINIMIZEBOX
            | Win32WindowNativeMethods.WS_MAXIMIZEBOX
        );
        Win32WindowNativeMethods.SetWindowLongPtr(targetHandle, Win32WindowNativeMethods.GWL_STYLE, style);

        Win32WindowNativeMethods.SetParent(targetHandle, containerHandle);

        // Use the container's current screen position (WS_POPUP ignores the parent origin).
        Win32WindowNativeMethods.GetWindowRect(containerHandle, out RECT containerRect);
        int w = Math.Max(1, width);
        int h = Math.Max(1, height);
        Win32WindowNativeMethods.MoveWindow(targetHandle, containerRect.Left, containerRect.Top, w, h, bRepaint: true);

        // Apply decoration style change (SWP_FRAMECHANGED) and show.
        Win32WindowNativeMethods.SetWindowPos(
            targetHandle,
            nint.Zero,
            containerRect.Left,
            containerRect.Top,
            w,
            h,
            Win32WindowNativeMethods.SWP_NOACTIVATE
                | Win32WindowNativeMethods.SWP_FRAMECHANGED
                | Win32WindowNativeMethods.SWP_SHOWWINDOW
        );

        // D3D11/GL swap chains don't automatically repaint after SetParent.
        // Sending WM_SIZE makes Qt/PCSX2 resize its render surface and redraw.
        nint lParam = (nint)(((h & 0xFFFF) << 16) | (w & 0xFFFF));
        Win32WindowNativeMethods.PostMessage(
            targetHandle,
            Win32WindowNativeMethods.WM_SIZE,
            Win32WindowNativeMethods.SIZE_RESTORED,
            lParam
        );
        Win32WindowNativeMethods.InvalidateRect(targetHandle, nint.Zero, true);
        Win32WindowNativeMethods.UpdateWindow(targetHandle);
    }

    public void ResizeEmbeddedWindow(nint targetHandle, int width, int height)
    {
        if (targetHandle == nint.Zero)
            return;

        int w = Math.Max(1, width);
        int h = Math.Max(1, height);

        // WS_POPUP windows use screen coordinates, not parent-relative coordinates.
        // Retrieve the container's current screen position via its Win32 parent handle.
        int x = 0;
        int y = 0;
        nint container = Win32WindowNativeMethods.GetParent(targetHandle);
        if (container != nint.Zero && Win32WindowNativeMethods.GetWindowRect(container, out RECT containerRect))
        {
            x = containerRect.Left;
            y = containerRect.Top;
        }

        Win32WindowNativeMethods.MoveWindow(targetHandle, x, y, w, h, bRepaint: true);

        // D3D/Qt swap chains don't repaint on MoveWindow alone — WM_SIZE triggers a render-target resize.
        nint lParam = (nint)(((h & 0xFFFF) << 16) | (w & 0xFFFF));
        Win32WindowNativeMethods.PostMessage(
            targetHandle,
            Win32WindowNativeMethods.WM_SIZE,
            Win32WindowNativeMethods.SIZE_RESTORED,
            lParam
        );
        Win32WindowNativeMethods.InvalidateRect(targetHandle, nint.Zero, true);
        Win32WindowNativeMethods.UpdateWindow(targetHandle);
    }

    public void ReleaseWindow(nint targetHandle)
    {
        if (targetHandle == nint.Zero)
            return;

        // Re-parent to desktop, restore a minimal set of decorations and show it.
        // WS_CHILD was never set during embedding (we kept WS_POPUP), so only restore decorations.
        Win32WindowNativeMethods.SetParent(targetHandle, nint.Zero);

        long style = Win32WindowNativeMethods.GetWindowLongPtr(targetHandle, Win32WindowNativeMethods.GWL_STYLE);
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
                | Win32WindowNativeMethods.SWP_NOMOVE
                | Win32WindowNativeMethods.SWP_NOSIZE
                | Win32WindowNativeMethods.SWP_NOACTIVATE
                | Win32WindowNativeMethods.SWP_FRAMECHANGED
                | Win32WindowNativeMethods.SWP_SHOWWINDOW
        );
    }

    // ── Diagnostics ───────────────────────────────────────────────────────────

    public string GetDiagnosticInfo(int pid)
    {
        var sb = new System.Text.StringBuilder();
        int total = 0;
        var cls = new System.Text.StringBuilder(256);
        var title = new System.Text.StringBuilder(256);

        Win32WindowNativeMethods.EnumWindows(
            (hwnd, _) =>
            {
                Win32WindowNativeMethods.GetWindowThreadProcessId(hwnd, out uint wpid);
                if ((int)wpid != pid)
                    return true;

                total++;
                bool visible = Win32WindowNativeMethods.IsWindowVisible(hwnd);
                Win32WindowNativeMethods.GetWindowRect(hwnd, out RECT r);

                cls.Clear();
                Win32WindowNativeMethods.GetClassName(hwnd, cls, cls.Capacity);

                title.Clear();
                Win32WindowNativeMethods.GetWindowText(hwnd, title, title.Capacity);

                sb.Append(
                    System.FormattableString.Invariant(
                        $"  0x{hwnd:X8}  {cls, -32}  {r.Width, 5}x{r.Height, -5}  visible={visible}  \"{title}\"\n"
                    )
                );
                return true;
            },
            nint.Zero
        );

        if (total == 0)
            return $"No top-level windows found for PID {pid}.";

        // Also enumerate child windows of every top-level window we found, so we can see
        // the actual D3D/GL render surface (which is a child in PCSX2-Qt).
        sb.Append("  Children:\n");
        Win32WindowNativeMethods.EnumWindows(
            (hwnd, _) =>
            {
                Win32WindowNativeMethods.GetWindowThreadProcessId(hwnd, out uint ownerPid);
                if ((int)ownerPid != pid)
                    return true;

                Win32WindowNativeMethods.EnumChildWindows(
                    hwnd,
                    (child, _) =>
                    {
                        bool childVisible = Win32WindowNativeMethods.IsWindowVisible(child);
                        Win32WindowNativeMethods.GetWindowRect(child, out RECT cr);
                        cls.Clear();
                        Win32WindowNativeMethods.GetClassName(child, cls, cls.Capacity);
                        title.Clear();
                        Win32WindowNativeMethods.GetWindowText(child, title, title.Capacity);
                        sb.Append(
                            System.FormattableString.Invariant(
                                $"    0x{child:X8}  {cls, -32}  {cr.Width, 5}x{cr.Height, -5}  visible={childVisible}  \"{title}\"\n"
                            )
                        );
                        return true;
                    },
                    nint.Zero
                );
                return true;
            },
            nint.Zero
        );

        return $"PID {pid} — {total} top-level window(s):\n{sb}";
    }

    // ── IDisposable ───────────────────────────────────────────────────────────

    public void Dispose() { }
}
