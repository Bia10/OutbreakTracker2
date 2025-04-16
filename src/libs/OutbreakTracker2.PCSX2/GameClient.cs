using System.ComponentModel;
using System.Diagnostics;
using OutbreakTracker2.WinInterop;
using OutbreakTracker2.WinInterop.Enums;

namespace OutbreakTracker2.PCSX2;

public sealed class GameClient : IDisposable
{
    private bool _disposed;
    public nint Handle { get; private set; }
    public nint MainModuleBase { get; private set; }
    public Process? Process { get; private set; }

    public void AttachToPCSX2(string processName = "pcsx2-qt")
    {
        Process[] processes = Process.GetProcessesByName(processName);
        if (processes.Length is 0) throw new Exception("Process not found");

        Process = processes[0];
        Handle = NativeMethods.OpenProcess(
            ProcessAccessFlags.VmRead | ProcessAccessFlags.QueryInformation,
            false,
            Process.Id
        );

        if (Handle == nint.Zero) throw new Win32Exception();

        MainModuleBase = Process.MainModule?.BaseAddress ?? throw new InvalidOperationException("MainModule not found");
    }

    public void Dispose()
    {
        if (_disposed) return;

        if (Handle != nint.Zero)
        {
            NativeMethods.CloseHandle(Handle);
            Handle = nint.Zero;
        }

        Process?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
