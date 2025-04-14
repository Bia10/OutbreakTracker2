using OutbreakTracker2.Memory;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.PCSX2Memory;
using System.Text;

namespace OutbreakTracker2.Outbreak;

public abstract class ReaderBase
{
    protected readonly GameClient GameClient;
    protected readonly EEmemMemory EEmemMemory;
    protected readonly IMemoryReader MemoryReader;
    protected readonly GameFile CurrentFile;

    protected ReaderBase(GameClient gameClient, EEmemMemory eememMemory)
    {
        GameClient = gameClient;
        EEmemMemory = eememMemory;
        MemoryReader = eememMemory.MemoryReader;

        CurrentFile = GetGameFile();
    }

    protected T Read<T>(nint address) where T : struct =>
        MemoryReader.Read<T>(GameClient.Handle, address);

    protected string ReadString(nint address, Encoding? encoding = null) =>
        MemoryReader.ReadString(GameClient.Handle, address, encoding);

    private byte GetGameFile()
    {
        byte f1Byte = Read<byte>(EEmemMemory.GetAddressFromPtr(FileOnePtrs.DiscStart));
        byte f2Byte = Read<byte>(EEmemMemory.GetAddressFromPtr(FileTwoPtrs.DiscStart));

        if (f1Byte == 0x53) return GameFile.FileOne;

        return f2Byte == 0x53 ? (byte)GameFile.FileTwo : (byte)GameFile.Unknown;
    }
}
