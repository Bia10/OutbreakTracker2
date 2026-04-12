using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Views.Common.Character;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGamePlayer.Factory;

public sealed class InGamePlayerViewModelFactory(
    ILogger<InGamePlayerViewModelFactory> logger,
    ICharacterBustViewModelFactory characterBustViewModelFactory,
    IInGamePlayerSubViewModelFactory inGamePlayerSubViewModelFactory
) : IInGamePlayerViewModelFactory
{
    private readonly ILogger<InGamePlayerViewModelFactory> _logger = logger;
    private readonly ICharacterBustViewModelFactory _characterBustViewModelFactory = characterBustViewModelFactory;
    private readonly IInGamePlayerSubViewModelFactory _inGamePlayerSubViewModelFactory =
        inGamePlayerSubViewModelFactory;

    public InGamePlayerViewModel Create(
        DecodedInGamePlayer playerData,
        byte currentGameFile,
        string scenarioName,
        DecodedItem[] scenarioItems
    )
    {
        if (playerData is null)
        {
            _logger.LogError("Player data is null. Cannot create InGamePlayerViewModel");
            throw new ArgumentNullException(nameof(playerData));
        }

        return new InGamePlayerViewModel(
            playerData,
            currentGameFile,
            scenarioName,
            scenarioItems,
            _characterBustViewModelFactory,
            _inGamePlayerSubViewModelFactory
        );
    }
}
