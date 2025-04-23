using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.PCSX2;

namespace OutbreakTracker2.App.Services.Data;

public interface IDataManager
{
    public DecodedDoor[] Doors { get; }

    public DecodedEnemy[] Enemies { get; }

    public DecodedInGamePlayer[] InGamePlayers { get; }

    public DecodedScenario InGameScenario { get; }

    public DecodedLobbyRoom LobbyRoom { get; }

    public DecodedLobbyRoomPlayer[] LobbyRoomPlayers { get; }

    public DecodedLobbySlot[] LobbySlots { get; }

    public void UpdateDoors();

    public void UpdateEnemies();

    public void UpdateInGamePlayer();

    public void UpdateInGameScenario();

    public void UpdateLobbyRoom();

    public void UpdateLobbyRoomPlayers();

    public void UpdateLobbySlots();

    public void UpdateAll(object? state);

    void Initialize(GameClient attachedGameClient);
}