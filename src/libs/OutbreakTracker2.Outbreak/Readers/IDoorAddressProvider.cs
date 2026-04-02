using OutbreakTracker2.Outbreak.Enums;

namespace OutbreakTracker2.Outbreak.Readers;

public interface IDoorAddressProvider
{
    GameFile SupportedFile { get; }

    int MaxDoors { get; }

    nint GetHealthAddress(int doorId);

    nint GetFlagAddress(int doorId);
}
