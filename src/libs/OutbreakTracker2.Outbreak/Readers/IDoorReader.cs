using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Outbreak.Readers;

public interface IDoorReader : IDisposable
{
    DecodedDoor[] DecodedDoors { get; }

    void UpdateDoors();
}
