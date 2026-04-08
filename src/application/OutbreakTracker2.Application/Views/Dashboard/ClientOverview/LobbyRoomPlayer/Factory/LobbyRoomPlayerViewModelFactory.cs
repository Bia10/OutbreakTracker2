using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Views.Common.Character;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.LobbyRoomPlayer.Factory;

public sealed class LobbyRoomPlayerViewModelFactory(
    ILogger<LobbyRoomPlayerViewModelFactory> logger,
    ICharacterBustViewModelFactory characterBustViewModelFactory
) : ILobbyRoomPlayerViewModelFactory
{
    private readonly ILogger<LobbyRoomPlayerViewModelFactory> _logger = logger;
    private readonly ICharacterBustViewModelFactory _characterBustViewModelFactory = characterBustViewModelFactory;

    public LobbyRoomPlayerViewModel Create(DecodedLobbyRoomPlayer playerData)
    {
        if (playerData is null)
        {
            _logger.LogError("Player data is null. Cannot create LobbyRoomPlayerViewModel");
            throw new ArgumentNullException(nameof(playerData));
        }

        CharacterBustViewModel newCharacterBustViewModel = _characterBustViewModelFactory.Create();
        LobbyRoomPlayerViewModel lobbyRoomPlayerVm = new(playerData, newCharacterBustViewModel);
        lobbyRoomPlayerVm.Update(playerData);
        return lobbyRoomPlayerVm;
    }
}
