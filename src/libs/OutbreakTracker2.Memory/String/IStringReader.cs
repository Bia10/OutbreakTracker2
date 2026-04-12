using System.Text;

namespace OutbreakTracker2.Memory.String;

public interface IStringReader
{
    public bool TryRead(nint hProcess, nint address, out string result, Encoding? encoding = null);

    public string Read(nint hProcess, nint address, Encoding? encoding = null);
}
