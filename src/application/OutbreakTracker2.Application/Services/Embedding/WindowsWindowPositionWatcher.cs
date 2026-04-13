using System.Runtime.Versioning;

namespace OutbreakTracker2.Application.Services.Embedding;

/// <summary>
/// Windows implementation of <see cref="IWindowPositionWatcher"/>.
/// Coordinates focused helper components for bounds synchronization, embedded HWND loss
/// detection, and mouse/foreground-driven focus routing without any background polling.
/// <remarks>
/// Both <see cref="Start"/> and <see cref="Stop"/> <b>must</b> be called from the Avalonia
/// UI thread — <c>SetWinEventHook</c>, <c>UnhookWinEvent</c>, <c>SetWindowsHookEx</c>,
/// and <c>UnhookWindowsHookEx</c> must all be called from the same thread.
/// </remarks>
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsWindowPositionWatcher : IWindowPositionWatcher
{
    private readonly WindowsEmbeddedWindowBoundsWatcher _boundsWatcher = new();
    private readonly WindowsEmbeddedWindowDestroyWatcher _destroyWatcher = new();
    private readonly WindowsEmbeddedWindowFocusRouter _focusRouter = new();

    public WindowsWindowPositionWatcher() => _destroyWatcher.EmbeddedWindowDestroyed += OnEmbeddedWindowDestroyed;

    public event EventHandler? EmbeddedWindowLost;

    public void Start(nint embeddedHandle, nint containerHandle, nint rootWindowHandle)
    {
        Stop();

        WindowsEmbeddedWindowSession session = new(embeddedHandle, containerHandle, rootWindowHandle);
        _boundsWatcher.Start(session);
        _destroyWatcher.Start(session);
        _focusRouter.Start(session, () => _boundsWatcher.CachedContainerRect);
    }

    public void Stop()
    {
        _focusRouter.Stop();
        _destroyWatcher.Stop();
        _boundsWatcher.Stop();
    }

    private void OnEmbeddedWindowDestroyed(object? sender, EventArgs eventArgs)
    {
        Stop();
        EmbeddedWindowLost?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        _destroyWatcher.EmbeddedWindowDestroyed -= OnEmbeddedWindowDestroyed;
        _focusRouter.Dispose();
        _destroyWatcher.Dispose();
        _boundsWatcher.Dispose();
    }
}
