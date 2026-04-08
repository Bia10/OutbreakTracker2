using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Outbreak.Readers;

public interface ILobbySlotReader : IDisposable
{
    DecodedLobbySlot[] DecodedLobbySlots { get; }

    void UpdateLobbySlots();
}
