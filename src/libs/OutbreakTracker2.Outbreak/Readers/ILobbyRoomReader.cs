using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Outbreak.Readers;

public interface ILobbyRoomReader : IDisposable
{
    DecodedLobbyRoom DecodedLobbyRoom { get; }

    void UpdateLobbyRoom();
}
