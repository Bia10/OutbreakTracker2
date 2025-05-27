using Microsoft.Extensions.Logging;
using OutbreakTracker2.App.Services.Data;
using OutbreakTracker2.App.Services.PlayerTracking;
using OutbreakTracker2.App.Views.Common.Character;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.Inventory.Factory;
using OutbreakTracker2.Outbreak.Models;
using System;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGamePlayer.Factory;

public class InGamePlayerViewModelFactory : IInGamePlayerViewModelFactory
{
    private readonly ILogger<InGamePlayerViewModelFactory> _logger;
    private readonly IDataManager _dataManager;
    private readonly ICharacterBustViewModelFactory _characterBustViewModelFactory;
private readonly IItemSlotViewModelFactory _itemSlotViewModelFactory;
private readonly IPlayerStateTracker _playerStateTracker;

    public InGamePlayerViewModelFactory(
        ILogger<InGamePlayerViewModelFactory> logger,
        IDataManager dataManager,
        ICharacterBustViewModelFactory characterBustViewModelFactory,
        IItemSlotViewModelFactory itemSlotViewModelFactory,
        IPlayerStateTracker playerStateTracker)
    {
        _logger = logger;
        _dataManager = dataManager;
        _characterBustViewModelFactory = characterBustViewModelFactory;
        _itemSlotViewModelFactory = itemSlotViewModelFactory;
        _playerStateTracker = playerStateTracker;
    }

    public InGamePlayerViewModel Create(DecodedInGamePlayer playerData)
    {
        if (playerData is null)
        {
            _logger.LogError("Player data is null. Cannot create InGamePlayerViewModel");
            throw new ArgumentNullException(nameof(playerData));
        }

        return new InGamePlayerViewModel(playerData, _dataManager, _characterBustViewModelFactory, _itemSlotViewModelFactory, _playerStateTracker);
    }
}