using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace OutbreakTracker2.LinuxInterop;

[StructLayout(LayoutKind.Sequential)]
public struct Iovec
{
    public nint iov_base;
    public nuint iov_len;
}

[SupportedOSPlatform("linux")]
public static partial class LinuxNativeMethods
{
    /// <summary>
    /// Reads memory from a remote process directly into a local buffer.
    /// Requires the calling process to be the parent of the target (or CAP_SYS_PTRACE).
    /// Returns the number of bytes read on success, or -1 on failure (errno set).
    /// </summary>
    [LibraryImport("libc", EntryPoint = "process_vm_readv", SetLastError = true)]
    public static partial long ProcessVmReadv(
        int pid,
        ref Iovec localIov,
        ulong liovcnt,
        ref Iovec remoteIov,
        ulong riovcnt,
        ulong flags
    );
}
