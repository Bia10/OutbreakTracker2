using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Views.Common.Character;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.Inventory.Factory;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGamePlayer.Factory;

public class InGamePlayerViewModelFactory(
    ILogger<InGamePlayerViewModelFactory> logger,
    IDataManager dataManager,
    ICharacterBustViewModelFactory characterBustViewModelFactory,
    IItemSlotViewModelFactory itemSlotViewModelFactory
) : IInGamePlayerViewModelFactory
{
    private readonly ILogger<InGamePlayerViewModelFactory> _logger = logger;
    private readonly IDataManager _dataManager = dataManager;
    private readonly ICharacterBustViewModelFactory _characterBustViewModelFactory = characterBustViewModelFactory;
    private readonly IItemSlotViewModelFactory _itemSlotViewModelFactory = itemSlotViewModelFactory;

    public InGamePlayerViewModel Create(DecodedInGamePlayer playerData)
    {
        if (playerData is null)
        {
            _logger.LogError("Player data is null. Cannot create InGamePlayerViewModel");
            throw new ArgumentNullException(nameof(playerData));
        }

        return new InGamePlayerViewModel(
            playerData,
            _dataManager,
            _characterBustViewModelFactory,
            _itemSlotViewModelFactory
        );
    }
}
