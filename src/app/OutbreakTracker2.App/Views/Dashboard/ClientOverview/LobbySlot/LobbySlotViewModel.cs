using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Models;
using System;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.LobbySlot;

public partial class LobbySlotViewModel : ObservableObject
{
    private DecodedLobbySlot _model;

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

    public bool IsPasswordProtectedBool
        => !string.IsNullOrEmpty(IsPassProtected) 
        && (IsPassProtected.Equals("true", StringComparison.Ordinal) || IsPassProtected == "1");

    public string PlayersDisplay => $"{CurPlayers}/{MaxPlayers}";

    public short UniqueSlotId => SlotNumber;

    public LobbySlotViewModel(DecodedLobbySlot model)
    {
        _model = model;
        Update(model);
    }

    public void Update(DecodedLobbySlot model)
    {
        _model = model;

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
           SlotNumber == viewModel.SlotNumber;

    public override int GetHashCode()
        => HashCode.Combine(SlotNumber);
}