using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OutbreakTracker2.Application.Views.Common.Character;

public sealed class CharacterBustViewModelFactory(
    ILogger<CharacterBustViewModelFactory> logger,
    IServiceProvider serviceProvider
) : ICharacterBustViewModelFactory
{
    private readonly ILogger<CharacterBustViewModelFactory> _logger = logger;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public CharacterBustViewModel Create()
    {
        _logger.LogTrace("Creating a new CharacterBustViewModel instance");

        CharacterBustViewModel newCharacterBustViewModel =
            _serviceProvider.GetRequiredService<CharacterBustViewModel>();

        return newCharacterBustViewModel;
    }
}
