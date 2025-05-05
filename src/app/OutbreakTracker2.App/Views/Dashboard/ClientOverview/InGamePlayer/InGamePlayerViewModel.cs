using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.App.Services.Data;
using OutbreakTracker2.App.Services.Dispatcher;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.Inventory;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGamePlayer;

public partial class InGamePlayerViewModel : ObservableObject
{
    [ObservableProperty]
    private string characterName = string.Empty;

    [ObservableProperty]
    private bool isEnabled;

    [ObservableProperty]
    private bool isInGame;

    [ObservableProperty]
    private PlayerGaugesViewModel _gauges;

    [ObservableProperty]
    private PlayerStatusEffectsViewModel _statusEffects;

    [ObservableProperty]
    private PlayerConditionsViewModel _conditions;

    [ObservableProperty]
    private PlayerAttributesViewModel _attributes;

    [ObservableProperty]
    private PlayerPositionViewModel _position;

    [ObservableProperty]
    private InventoryViewModel _inventory;

    private readonly IDispatcherService _dispatcherService;

    public InGamePlayerViewModel(DecodedInGamePlayer player, IDataManager dataManager, IDispatcherService dispatcherService)
    {
        _dispatcherService = dispatcherService;
        _gauges = new PlayerGaugesViewModel();
        _statusEffects = new PlayerStatusEffectsViewModel();
        _conditions = new PlayerConditionsViewModel();
        _attributes = new PlayerAttributesViewModel();
        _position = new PlayerPositionViewModel(dataManager);
        _inventory = new InventoryViewModel(dataManager);

        Update(player);
    }

    public void Update(DecodedInGamePlayer player)
    {
        if (!_dispatcherService.IsOnUIThread())
            _dispatcherService.PostOnUI(() => UpdateProperties(player));
        else
            UpdateProperties(player);
    }

    private void UpdateProperties(DecodedInGamePlayer player)
    {
        CharacterName = player.CharacterName;
        IsEnabled = player.Enabled;
        IsInGame = player.InGame;

        Gauges.Update(player.CurrentHealth, player.MaximumHealth, player.HealthPercentage, player.CurVirus, player.MaxVirus, player.VirusPercentage);
        StatusEffects.Update(player.BleedTime, player.AntiVirusTime, player.AntiVirusGTime, player.HerbTime);
        Conditions.Update(player.Condition, player.Status);
        Attributes.Update(player.CritBonus, player.Size, player.Power, player.Speed);
        Position.Update(player.PositionX, player.PositionY, player.RoomId);

        Inventory.UpdateFromPlayerData(
            player.EquippedItem,
            player.Inventory,
            player.SpecialItem,
            player.SpecialInventory,
            player.DeadInventory,
            player.SpecialDeadInventory
        );
    }
}