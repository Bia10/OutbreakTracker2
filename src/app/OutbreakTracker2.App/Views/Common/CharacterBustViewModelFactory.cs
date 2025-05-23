using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace OutbreakTracker2.App.Views.Common;

public sealed class CharacterBustViewModelFactory : ICharacterBustViewModelFactory
{
    private readonly ILogger<CharacterBustViewModelFactory> _logger;
    private readonly IServiceProvider _serviceProvider;

    public CharacterBustViewModelFactory(
        ILogger<CharacterBustViewModelFactory> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public CharacterBustViewModel Create()
    {
        _logger.LogDebug("Creating a new CharacterBustViewModel instance");

        CharacterBustViewModel newCharacterBustViewModel =
            _serviceProvider.GetRequiredService<CharacterBustViewModel>();

        _logger.LogDebug("CharacterBustViewModel created successfully");
        return newCharacterBustViewModel;
    }
}