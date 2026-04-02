namespace OutbreakTracker2.Application.Services.Embedding;

/// <summary>
/// Fallback <see cref="IWindowEmbedder"/> for platforms where window embedding is not supported
/// (macOS, Wayland-only environments without <c>DISPLAY</c>, etc.).
/// All methods are safe no-ops.
/// </summary>
internal sealed class NullWindowEmbedder : IWindowEmbedder
{
    public bool IsSupported => false;

    public nint CreateContainerWindow(nint parentHandle, int width, int height) => nint.Zero;

    public void DestroyContainerWindow(nint containerHandle) { }

    public nint FindProcessWindow(int pid) => nint.Zero;

    public void EmbedWindow(nint containerHandle, nint targetHandle, int width, int height) { }

    public void ResizeEmbeddedWindow(nint targetHandle, int width, int height) { }

    public void ReleaseWindow(nint targetHandle) { }

    public string GetDiagnosticInfo(int pid) => string.Empty;

    public void Dispose() { }
}
