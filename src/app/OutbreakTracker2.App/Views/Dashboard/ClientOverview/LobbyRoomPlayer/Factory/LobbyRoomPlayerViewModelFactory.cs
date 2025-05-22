using Microsoft.Extensions.Logging;
using OutbreakTracker2.App.Services.Dispatcher;
using OutbreakTracker2.App.Services.TextureAtlas;
using OutbreakTracker2.App.Views.Common;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.LobbyRoomPlayer.Factory;

public class LobbyRoomPlayerViewModelFactory : ILobbyRoomPlayerViewModelFactory
{
    private readonly ILogger<LobbyRoomPlayerViewModelFactory> _logger;
    private readonly ITextureAtlas _textureAtlas;
    private readonly IDispatcherService _dispatcherService;
    private readonly ILogger<CharacterBustViewModel> _characterBustLogger;

    public LobbyRoomPlayerViewModelFactory(
        ILogger<LobbyRoomPlayerViewModelFactory> logger,
        ITextureAtlas textureAtlas,
        IDispatcherService dispatcherService,
        ILogger<CharacterBustViewModel> characterBustLogger)
    {
        _logger = logger;
        _textureAtlas = textureAtlas;
        _dispatcherService = dispatcherService;
        _characterBustLogger = characterBustLogger;
    }

    public LobbyRoomPlayerViewModel Create(DecodedLobbyRoomPlayer playerData)
    {
        CharacterBustViewModel newCharacterBustViewModel = new(
            _characterBustLogger,
            _textureAtlas,
            _dispatcherService
        );

        LobbyRoomPlayerViewModel vm = new(playerData, newCharacterBustViewModel);
        vm.Update(playerData);
        return vm;
    }
}