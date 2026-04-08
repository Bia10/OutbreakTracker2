using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Outbreak.Readers;

public interface IInGamePlayerReader : IDisposable
{
    DecodedInGamePlayer[] DecodedInGamePlayers { get; }

    void UpdateInGamePlayers();
}
