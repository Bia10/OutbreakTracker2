using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Outbreak.Readers;

public interface ILobbyRoomPlayerReader : IDisposable
{
    DecodedLobbyRoomPlayer[] DecodedLobbyRoomPlayers { get; }

    void UpdateRoomPlayers();
}
