using System.Text;

namespace OutbreakTracker2.Memory.String;

public interface IStringReader
{
    public string Read(nint hProcess, nint address, Encoding? encoding = null);
}