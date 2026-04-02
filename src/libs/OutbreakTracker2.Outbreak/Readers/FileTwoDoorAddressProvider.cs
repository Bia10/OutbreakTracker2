using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;

namespace OutbreakTracker2.Outbreak.Readers;

public sealed class FileTwoDoorAddressProvider : IDoorAddressProvider
{
    public GameFile SupportedFile => GameFile.FileTwo;

    public int MaxDoors => GameConstants.MaxDoors;

    public nint GetHealthAddress(int doorId) => FileTwoPtrs.GetDoorHealthAddress(doorId);

    public nint GetFlagAddress(int doorId) => FileTwoPtrs.GetDoorFlagAddress(doorId);
}
