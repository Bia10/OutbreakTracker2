using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Models;
using System;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.LobbySlot;

public partial class LobbySlotViewModel : ObservableObject
{
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

    public LobbySlotViewModel(DecodedLobbySlot model)
    {
        Update(model);
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
    }

    public override bool Equals(object? obj)
        => obj is LobbySlotViewModel viewModel &&
           Id == viewModel.Id;

    public override int GetHashCode()
        => HashCode.Combine(Id);
}