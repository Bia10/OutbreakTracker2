using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;

namespace OutbreakTracker2.Outbreak.Readers;

public sealed class FileOneDoorAddressProvider : IDoorAddressProvider
{
    public GameFile SupportedFile => GameFile.FileOne;

    public int MaxDoors => GameConstants.MaxDoors - 9;

    public nint GetHealthAddress(int doorId) => FileOnePtrs.GetDoorHealthAddress(doorId);

    public nint GetFlagAddress(int doorId) => FileOnePtrs.GetDoorFlagAddress(doorId);
}
