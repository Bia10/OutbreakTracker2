using System.Runtime.Versioning;
using OutbreakTracker2.WinInterop;

namespace OutbreakTracker2.Application.Services.Embedding;

[SupportedOSPlatform("windows")]
internal sealed class WindowsEmbeddedWindowSession
{
    public WindowsEmbeddedWindowSession(nint embeddedHandle, nint containerHandle, nint rootWindowHandle)
    {
        EmbeddedHandle = embeddedHandle;
        ContainerHandle = containerHandle;
        RootWindowHandle = rootWindowHandle;
        UiThreadId = Win32WindowNativeMethods.GetCurrentThreadId();
        Pcsx2ThreadId = Win32WindowNativeMethods.GetWindowThreadProcessId(embeddedHandle, out uint pcsx2ProcessId);
        Pcsx2ProcessId = pcsx2ProcessId;
    }

    public nint EmbeddedHandle { get; }

    public nint ContainerHandle { get; }

    public nint RootWindowHandle { get; }

    public uint UiThreadId { get; }

    public uint Pcsx2ThreadId { get; }

    public uint Pcsx2ProcessId { get; }
}
