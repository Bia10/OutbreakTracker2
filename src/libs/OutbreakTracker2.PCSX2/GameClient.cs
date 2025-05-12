using OutbreakTracker2.WinInterop;
using OutbreakTracker2.WinInterop.Enums;
using System.ComponentModel;
using System.Diagnostics;

namespace OutbreakTracker2.PCSX2;

public sealed class GameClient : IDisposable
{
    private bool _disposed;

    public nint Handle { get; private set; }

    public nint MainModuleBase { get; private set; }

    public Process? Process { get; private set; }

    public bool IsAttached => Handle != nint.Zero;

    public void Attach(Process process)
    {
        Process = process ?? throw new ArgumentNullException(nameof(process));
        Handle = NativeMethods.OpenProcess(
            ProcessAccessFlags.VmRead | ProcessAccessFlags.QueryInformation,
            false,
            Process.Id
        );

        if (!IsAttached) throw new Win32Exception();

        MainModuleBase = Process.MainModule?.BaseAddress
                         ?? throw new InvalidOperationException("MainModule not found");
    }

    public void Dispose()
    {
        if (_disposed) return;

        if (IsAttached)
        {
            NativeMethods.CloseHandle(Handle);
            Handle = nint.Zero;
        }

        Process?.Dispose();
        _disposed = true;
    }
}
