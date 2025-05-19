using System.Runtime.InteropServices;

namespace OutbreakTracker2.Benchmarks;

// Helper class containing the necessary P/Invoke signatures
// We need two ReadProcessMemory signatures: one for byte[] (safe), one for void* (unsafe)
public static partial class Kernel32
{
    [LibraryImport("kernel32.dll", EntryPoint = "OpenProcess", SetLastError = true)]
    public static partial nint OpenProcess(ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

    // Signature used by the "safe" version
    [LibraryImport("kernel32.dll", EntryPoint = "ReadProcessMemory", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ReadProcessMemory_Safe(nint hProcess, nint lpBaseAddress, [Out] byte[] lpBuffer, int nSize, out int lpNumberOfBytesRead);

    // Signature used by the "unsafe" version
    [LibraryImport("kernel32.dll", EntryPoint = "ReadProcessMemory", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool ReadProcessMemory_Unsafe(nint hProcess, nint lpBaseAddress, void* lpBuffer, int nSize, out int lpNumberOfBytesRead);

    [LibraryImport("kernel32.dll", EntryPoint = "CloseHandle", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseHandle(nint handle);
}