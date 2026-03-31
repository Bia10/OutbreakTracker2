namespace OutbreakTracker2.Application.Services.Embedding;

/// <summary>
/// Cross-platform abstraction for embedding a child-process window into a native container.
/// <list type="bullet">
///   <item><description>Linux/X11: uses <c>XReparentWindow</c> from libX11.</description></item>
///   <item><description>Windows: uses <c>SetParent</c> from user32.</description></item>
///   <item><description>Unsupported platforms: <see cref="IsSupported"/> returns <see langword="false"/>
///     and all methods are no-ops.</description></item>
/// </list>
/// The implementation is registered as a singleton and holds any OS-level resources (e.g. an
/// <c>X11 Display</c> connection) for its lifetime.
/// </summary>
public interface IWindowEmbedder : IDisposable
{
    /// <summary>
    /// <see langword="true"/> when window embedding is available on the current platform.
    /// Linux: requires an active <c>DISPLAY</c> connection (X11 / XWayland).
    /// Windows: always <see langword="true"/>.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Creates a native container window as a child of <paramref name="parentHandle"/>.
    /// The returned handle is passed to Avalonia as the <c>NativeControlHost</c> child handle,
    /// and to <see cref="EmbedWindow"/> as the embedding target.
    /// </summary>
    nint CreateContainerWindow(nint parentHandle, int width, int height);

    /// <summary>Destroys a container window previously created with <see cref="CreateContainerWindow"/>.</summary>
    void DestroyContainerWindow(nint containerHandle);

    /// <summary>
    /// Searches for the main window of the process identified by <paramref name="pid"/>.
    /// Returns <see cref="nint.Zero"/> if no suitable window is found yet.
    /// This method is safe to call from a background thread.
    /// </summary>
    nint FindProcessWindow(int pid);

    /// <summary>
    /// Reparents <paramref name="targetHandle"/> into <paramref name="containerHandle"/> and resizes it to fill.
    /// Must be called on the same thread that opened the display (Linux: after <c>XInitThreads</c>).
    /// </summary>
    void EmbedWindow(nint containerHandle, nint targetHandle, int width, int height);

    /// <summary>Resizes the previously embedded window to the new dimensions.</summary>
    void ResizeEmbeddedWindow(nint targetHandle, int width, int height);

    /// <summary>
    /// Releases an embedded window by reparenting it back to the desktop root (Linux) or
    /// clearing its parent (Windows). Call before destroying the container.
    /// </summary>
    void ReleaseWindow(nint targetHandle);
}
