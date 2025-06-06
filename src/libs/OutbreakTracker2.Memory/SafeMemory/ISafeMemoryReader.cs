namespace OutbreakTracker2.Memory.SafeMemory;

public interface ISafeMemoryReader
{
    public T Read<T>(nint hProcess, nint address) where T : unmanaged;

    public T ReadStruct<T>(nint hProcess, nint address) where T : struct;
}
