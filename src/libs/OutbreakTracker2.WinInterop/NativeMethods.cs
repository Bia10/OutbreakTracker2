namespace OutbreakTracker2.WinInterop;

public static class NativeMethods
{
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, EntryPoint = "OpenProcess", SetLastError = true)]
    internal static extern nint OpenProcess(ProcessAccessFlags dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, EntryPoint = "ReadProcessMemory", SetLastError = true)]
    internal static extern bool ReadProcessMemory(nint hProcess, nint lpBaseAddress, [Out] byte[] lpBuffer, int nSize, out int lpNumberOfBytesRead);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, EntryPoint = "WriteProcessMemory", SetLastError = true)]
    internal static extern bool WriteProcessMemory(nint hProcess, nint lpBaseAddress, byte[] lpBuffer, int nSize, nint lpNumberOfBytesWritten);

    [DllImport("kernel32.dll", EntryPoint = "CloseHandle", SetLastError = true)]
    internal static extern bool CloseHandle(nint handle);
}
