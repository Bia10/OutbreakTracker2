using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.LobbyRoomPlayer.Factory;

public interface ILobbyRoomPlayerViewModelFactory
{
    public LobbyRoomPlayerViewModel Create(DecodedLobbyRoomPlayer playerData);
}