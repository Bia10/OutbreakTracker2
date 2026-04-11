using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Services.Atlas;

namespace OutbreakTracker2.Application.Views.Common.Character;

public sealed class CharacterBustViewModelFactory(
    ILogger<CharacterBustViewModelFactory> logger,
    ILogger<CharacterBustViewModel> characterBustViewModelLogger,
    ISpriteNameResolver spriteNameResolver,
    IImageViewModelFactory imageViewModelFactory
) : ICharacterBustViewModelFactory
{
    private readonly ILogger<CharacterBustViewModelFactory> _logger = logger;
    private readonly ILogger<CharacterBustViewModel> _characterBustViewModelLogger = characterBustViewModelLogger;
    private readonly ISpriteNameResolver _spriteNameResolver = spriteNameResolver;
    private readonly IImageViewModelFactory _imageViewModelFactory = imageViewModelFactory;

    public CharacterBustViewModel Create()
    {
        _logger.LogTrace("Creating a new CharacterBustViewModel instance");
        return new CharacterBustViewModel(_characterBustViewModelLogger, _spriteNameResolver, _imageViewModelFactory);
    }
}
