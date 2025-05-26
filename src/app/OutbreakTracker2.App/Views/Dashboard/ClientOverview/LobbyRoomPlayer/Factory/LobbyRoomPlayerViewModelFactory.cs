using Microsoft.Extensions.Logging;
using OutbreakTracker2.App.Views.Common.Character;
using OutbreakTracker2.Outbreak.Models;
using System;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.LobbyRoomPlayer.Factory;

public class LobbyRoomPlayerViewModelFactory : ILobbyRoomPlayerViewModelFactory
{
    private readonly ILogger<LobbyRoomPlayerViewModelFactory> _logger;
    private readonly ICharacterBustViewModelFactory _characterBustViewModelFactory;

    public LobbyRoomPlayerViewModelFactory(
        ILogger<LobbyRoomPlayerViewModelFactory> logger,
        ICharacterBustViewModelFactory characterBustViewModelFactory)
    {
        _logger = logger;
        _characterBustViewModelFactory = characterBustViewModelFactory;
    }

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