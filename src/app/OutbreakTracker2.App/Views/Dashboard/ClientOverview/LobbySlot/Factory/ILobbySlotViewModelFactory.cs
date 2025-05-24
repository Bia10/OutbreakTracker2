using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.LobbySlot.Factory;

public interface ILobbySlotViewModelFactory
{
    LobbySlotViewModel Create(DecodedLobbySlot initialData);
}