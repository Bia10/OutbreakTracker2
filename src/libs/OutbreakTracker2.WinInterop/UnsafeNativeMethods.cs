using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace OutbreakTracker2.WinInterop;

[SupportedOSPlatform("windows")]
public static partial class UnsafeNativeMethods
{
    [LibraryImport("kernel32.dll", EntryPoint = "ReadProcessMemory", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool ReadProcessMemory(
        nint hProcess,
        nint lpBaseAddress,
        void* lpBuffer,
        int nSize,
        out int lpNumberOfBytesRead
    );
}
