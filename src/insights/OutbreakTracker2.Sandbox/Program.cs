using OutbreakTracker2.PCSX2Memory;

namespace OutbreakTracker2.Sandbox;

public class Program
{
    static void Main(string[] _)
    {
        EEmemMemory memory = new("pcsx2-qt");

        if (memory.EEmemBaseAddress != nint.Zero)
            Console.WriteLine($"EEmem base address: 0x{memory.EEmemBaseAddress.ToInt64():X}");

        if (memory.EEmemBaseAddress == nint.Zero)
        {
            Console.WriteLine("EEmem base address not found.");
            return;
        }
    }
}
