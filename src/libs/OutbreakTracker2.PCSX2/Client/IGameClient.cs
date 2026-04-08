using System.Diagnostics;
using System.Runtime.Versioning;

namespace OutbreakTracker2.PCSX2.Client;

public interface IGameClient : IDisposable
{
    /// <summary>
    /// Windows: Win32 process handle from <c>OpenProcess</c>.
    /// Linux: process ID cast to <see cref="nint"/>. No OS handle is held.
    /// </summary>
    nint Handle { get; }

    bool IsAttached { get; }

    Process? Process { get; }

    /// <summary>
    /// Windows only: base address of the main module.
    /// </summary>
    [SupportedOSPlatform("windows")]
    nint MainModuleBase { get; }
}
