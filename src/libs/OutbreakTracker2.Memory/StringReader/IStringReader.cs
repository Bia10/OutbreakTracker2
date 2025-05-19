using System.Text;

namespace OutbreakTracker2.Memory.StringReader;

public interface IStringReader
{
    public string Read(nint hProcess, nint address, Encoding? encoding = null);
}