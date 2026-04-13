using Avalonia.Media;
using Avalonia.Media.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Views.Common.ScenarioImg;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Utility;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.LobbySlot;

public sealed partial class LobbySlotViewModel : ObservableObject
{
    private readonly ILogger<LobbySlotViewModel> _logger;

    private static readonly IBrush LockedBrush = new ImmutableSolidColorBrush(Colors.Red);
    private static readonly IBrush UnlockedBrush = new ImmutableSolidColorBrush(Colors.LimeGreen);

    [ObservableProperty]
    private Ulid _id = Ulid.NewUlid();

    [ObservableProperty]
    private short _slotNumber;

    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlayersDisplay))]
    [NotifyPropertyChangedFor(nameof(LockForeground))]
    private bool _isPassProtected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlayersDisplay))]
    private short _curPlayers;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlayersDisplay))]
    private short _maxPlayers = GameConstants.MaxPlayers;

    [ObservableProperty]
    private string _scenarioId = string.Empty;

    [ObservableProperty]
    private string _version = string.Empty;

    [ObservableProperty]
    private string _title = string.Empty;

    public string PlayersDisplay => $"{CurPlayers}/{MaxPlayers}";

    public IBrush LockForeground => IsPassProtected ? LockedBrush : UnlockedBrush;

    public Ulid UniqueSlotId => Id;

    public ScenarioImageViewModel ScenarioImageViewModel { get; }

    public LobbySlotViewModel(
        ILogger<LobbySlotViewModel> logger,
        IScenarioImageViewModelFactory scenarioImageViewModelFactory,
        DecodedLobbySlot initialData
    )
    {
        _logger = logger;

        ScenarioImageViewModel = scenarioImageViewModelFactory.Create();

        Update(initialData);
    }

    public void Update(in DecodedLobbySlot model)
    {
        SlotNumber = model.SlotNumber;
        Status = model.Status;
        IsPassProtected = model.IsPassProtected;
        CurPlayers = model.CurPlayers;
        MaxPlayers = model.MaxPlayers;
        ScenarioId = model.ScenarioId;
        Version = model.Version;
        Title = model.Title;

        if (EnumUtility.TryParseByValueOrMember(ScenarioId, out Scenario scenarioType))
        {
            _ = TrackScenarioImageUpdateAsync(ScenarioImageViewModel.UpdateImageAsync(scenarioType), ScenarioId);
        }
        else
        {
            _logger.LogWarning(
                "ScenarioName '{ScenarioName}' could not be parsed to a ScenarioType. Displaying default image",
                ScenarioId
            );
            _ = TrackScenarioImageUpdateAsync(ScenarioImageViewModel.UpdateToDefaultImageAsync(), ScenarioId);
        }
    }

    private async Task TrackScenarioImageUpdateAsync(ValueTask updateTask, string scenarioId)
    {
        try
        {
            await updateTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update scenario image for lobby slot {ScenarioId}", scenarioId);
        }
    }

    public override bool Equals(object? obj) => obj is LobbySlotViewModel viewModel && Id == viewModel.Id;

    public override int GetHashCode() => HashCode.Combine(Id);
}
