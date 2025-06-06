using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Views.Common.ScenarioImg;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Utility;
using System;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.LobbySlot;

public partial class LobbySlotViewModel : ObservableObject
{
    private readonly ILogger<LobbySlotViewModel> _logger;

    [ObservableProperty]
    private Ulid _id = Ulid.NewUlid();

    [ObservableProperty]
    private short _slotNumber;

    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    private string _isPassProtected = string.Empty;

    [ObservableProperty]
    private short _curPlayers;

    [ObservableProperty]
    private short _maxPlayers = GameConstants.MaxPlayers;

    [ObservableProperty]
    private string _scenarioId = string.Empty;

    [ObservableProperty]
    private string _version = string.Empty;

    [ObservableProperty]
    private string _title = string.Empty;

    public bool IsPasswordProtectedBool => !string.IsNullOrEmpty(IsPassProtected)
                                           && (IsPassProtected.Equals("true", StringComparison.Ordinal)
                                               || IsPassProtected.Equals("1", StringComparison.Ordinal));

    public string PlayersDisplay => $"{CurPlayers}/{MaxPlayers}";

    public Ulid UniqueSlotId => Id;

    public ScenarioImageViewModel ScenarioImageViewModel { get; }

    public LobbySlotViewModel(
        ILogger<LobbySlotViewModel> logger,
        IScenarioImageViewModelFactory scenarioImageViewModelFactory,
        DecodedLobbySlot initialData)
    {
        _logger = logger;

        ScenarioImageViewModel = scenarioImageViewModelFactory.Create();

        Update(initialData);

        _logger.LogInformation("LobbySlotViewModel initialized for ULID: {Ulid}", _id);
    }

    public void Update(DecodedLobbySlot model)
    {
        SlotNumber = model.SlotNumber;
        Status = model.Status;
        IsPassProtected = model.IsPassProtected;
        CurPlayers = model.CurPlayers;
        MaxPlayers = model.MaxPlayers;
        ScenarioId = model.ScenarioId;
        Version = model.Version;
        Title = model.Title;

        OnPropertyChanged(nameof(IsPasswordProtectedBool));
        OnPropertyChanged(nameof(PlayersDisplay));

        if (EnumUtility.TryParseByValueOrMember(ScenarioId, out Scenario scenarioType))
        {
            _ = ScenarioImageViewModel.UpdateImageAsync(scenarioType);
        }
        else
        {
            _logger.LogWarning("ScenarioName '{ScenarioName}' could not be parsed to a ScenarioType. Displaying default image", ScenarioId);
            _ = ScenarioImageViewModel.UpdateToDefaultImageAsync();
        }
    }

    public override bool Equals(object? obj)
        => obj is LobbySlotViewModel viewModel &&
           Id == viewModel.Id;

    public override int GetHashCode()
        => HashCode.Combine<Ulid>(Id);
}