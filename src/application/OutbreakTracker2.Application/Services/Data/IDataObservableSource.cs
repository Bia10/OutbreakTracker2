using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Services.Data;

/// <summary>
/// Push-based observable streams. Inject this into ViewModels and TrackerRegistry.
/// </summary>
public interface IDataObservableSource
{
    Observable<DecodedDoor[]> DoorsObservable { get; }
    Observable<DecodedEnemy[]> EnemiesObservable { get; }
    Observable<DecodedInGamePlayer[]> InGamePlayersObservable { get; }
    Observable<DecodedInGameScenario> InGameScenarioObservable { get; }
    Observable<DecodedLobbyRoom> LobbyRoomObservable { get; }
    Observable<DecodedLobbyRoomPlayer[]> LobbyRoomPlayersObservable { get; }
    Observable<DecodedLobbySlot[]> LobbySlotsObservable { get; }
    Observable<bool> IsAtLobbyObservable { get; }
}
