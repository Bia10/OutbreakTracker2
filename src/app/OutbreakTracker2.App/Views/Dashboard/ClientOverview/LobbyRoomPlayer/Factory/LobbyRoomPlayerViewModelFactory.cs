using Microsoft.Extensions.Logging;
using OutbreakTracker2.App.Services.Dispatcher;
using OutbreakTracker2.App.Services.TextureAtlas;
using OutbreakTracker2.App.Views.Common;
using OutbreakTracker2.Outbreak.Models;
using System;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.LobbyRoomPlayer.Factory;

public class LobbyRoomPlayerViewModelFactory : ILobbyRoomPlayerViewModelFactory
{
    private readonly ILogger<LobbyRoomPlayerViewModelFactory> _logger;
    private readonly ITextureAtlasService _textureAtlasService;
    private readonly IDispatcherService _dispatcherService;
    private readonly ILogger<CharacterBustViewModel> _characterBustLogger;

    public LobbyRoomPlayerViewModelFactory(
        ILogger<LobbyRoomPlayerViewModelFactory> logger,
        ITextureAtlasService textureAtlasService,
        IDispatcherService dispatcherService,
        ILogger<CharacterBustViewModel> characterBustLogger)
    {
        _logger = logger;
        _textureAtlasService = textureAtlasService;
        _dispatcherService = dispatcherService;
        _characterBustLogger = characterBustLogger;
    }

    public LobbyRoomPlayerViewModel Create(DecodedLobbyRoomPlayer playerData)
    {
        if (playerData is null)
        {
            _logger.LogError("Player data is null. Cannot create LobbyRoomPlayerViewModel");
            throw new ArgumentNullException(nameof(playerData));
        }

        CharacterBustViewModel newCharacterBustViewModel = new(
            _characterBustLogger,
            _textureAtlasService,
            _dispatcherService
        );

        LobbyRoomPlayerViewModel lobbyRoomPlayerVm = new(playerData, newCharacterBustViewModel);
        lobbyRoomPlayerVm.Update(playerData);
        return lobbyRoomPlayerVm;
    }
}