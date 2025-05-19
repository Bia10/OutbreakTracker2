namespace OutbreakTracker2.Memory.MemoryReader;

public interface IUnsafeMemoryReader
{
    public unsafe T Read<T>(nint hProcess, nint address) where T : unmanaged;
}