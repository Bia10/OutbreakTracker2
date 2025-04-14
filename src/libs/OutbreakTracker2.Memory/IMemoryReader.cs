namespace OutbreakTracker2.Memory;

public interface IMemoryReader
{
    public T Read<T>(nint hProcess, nint address) where T : struct;

    public string ReadString(nint hProcess, nint address, Encoding? encoding = null);
}