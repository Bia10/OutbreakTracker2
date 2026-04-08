using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;
using OutbreakTracker2.WinInterop;
using OutbreakTracker2.WinInterop.Enums;

namespace OutbreakTracker2.PCSX2.Client;

public sealed class GameClient : IGameClient
{
    private bool _disposed;

    /// <summary>
    /// Windows: Win32 process handle from <c>OpenProcess</c>.
    /// Linux: process ID cast to <see cref="nint"/>. No OS handle is held.
    /// </summary>
    public nint Handle { get; private set; }

    /// <summary>
    /// Windows: fresh base address of the main module, re-read on every access so that
    /// <see cref="OutbreakTracker2.PCSX2.EEmem.EEmemMemory"/> retry loops see the correct
    /// value once the process finishes loading its modules.
    /// Linux: not supported — EEmem resolution uses <c>/proc/&lt;pid&gt;/maps</c> directly.
    /// </summary>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public nint MainModuleBase => Process?.MainModule?.BaseAddress ?? nint.Zero;

    public Process? Process { get; private set; }

    public bool IsAttached => Handle != nint.Zero;

    public void Attach(Process process)
    {
        Process = process ?? throw new ArgumentNullException(nameof(process));

        if (OperatingSystem.IsWindows())
            AttachWindows();
        else if (OperatingSystem.IsLinux())
            AttachLinux();
        else
            throw new PlatformNotSupportedException("GameClient.Attach is only supported on Windows and Linux.");
    }

    [SupportedOSPlatform("windows")]
    private void AttachWindows()
    {
        Handle = SafeNativeMethods.OpenProcess(
            ProcessAccessFlags.VmRead | ProcessAccessFlags.QueryInformation,
            bInheritHandle: false,
            Process!.Id
        );

        if (!IsAttached)
            throw new Win32Exception();
    }

    [SupportedOSPlatform("linux")]
    private void AttachLinux()
    {
        // On Linux we use the PID directly — no OS handle is opened.
        // process_vm_readv only requires the calling process to be the parent
        // (ptrace_scope = 1, Ubuntu default) or have CAP_SYS_PTRACE.
        Handle = (nint)Process!.Id;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (IsAttached && OperatingSystem.IsWindows())
            CloseHandleWindows();

        Handle = nint.Zero;
        Process = null;
        _disposed = true;
    }

    [SupportedOSPlatform("windows")]
    private void CloseHandleWindows()
    {
        SafeNativeMethods.CloseHandle(Handle);
    }

    /// <summary>
    /// Returns a human-readable string describing the attached process.
    /// Used for diagnostics only — do not parse the output.
    /// </summary>
    public override string ToString()
    {
        if (!IsAttached)
            return "GameClient[not attached]";

        string baseInfo = OperatingSystem.IsWindows() ? $"Base=0x{MainModuleBase:X}" : $"PID={Process?.Id}";

        return $"GameClient[PID={Process?.Id}, {baseInfo}]";
    }
}
