namespace OutbreakTracker2.Application.Services.Embedding;

/// <summary>
/// Continuously monitors the screen position of a native container window and keeps an
/// embedded child window visually synchronised with it.
/// <para>
/// Responsibilities:
/// <list type="bullet">
///   <item><description>
///     Hide the embedded window when the container is hidden (prevents a WS_POPUP window from
///     floating over unrelated UI when the user switches tabs).
///   </description></item>
///   <item><description>
///     Show and reposition the embedded window when the container becomes visible again, or when
///     the container moves/resizes (e.g. user drags the host application window).
///   </description></item>
///   <item><description>
///     Stop automatically if either the container or the embedded window is destroyed.
///   </description></item>
/// </list>
/// </para>
/// </summary>
public interface IWindowPositionWatcher : IDisposable
{
    /// <summary>
    /// Starts the watch loop. If already running, restarts with the new handles.
    /// </summary>
    /// <param name="embeddedHandle">
    /// Handle of the process window being embedded (e.g. PCSX2's main window).
    /// </param>
    /// <param name="containerHandle">
    /// Handle of the Win32 container created by <see cref="IWindowEmbedder.CreateContainerWindow"/>.
    /// </param>
    void Start(nint embeddedHandle, nint containerHandle);

    /// <summary>Stops the watch loop. Safe to call when not started.</summary>
    void Stop();
}
