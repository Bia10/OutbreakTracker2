﻿using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Views.Common.Character;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.Inventory;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.Inventory.Factory;
using OutbreakTracker2.Outbreak.Enums.Character;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Utility;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGamePlayer;

public partial class InGamePlayerViewModel : ObservableObject
{
    [ObservableProperty]
    private short _nameId;

    [ObservableProperty]
    private string _characterName = string.Empty;

    [ObservableProperty]
    private string _characterType = string.Empty;

    [ObservableProperty]
    private string _uniqueNameId = string.Empty;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private bool _isInGame;

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

    [ObservableProperty]
    private CharacterBustViewModel _playerBustViewModel;

    private readonly IDataManager _dataManager;

    public InGamePlayerViewModel(
        DecodedInGamePlayer player,
        IDataManager dataManager,
        ICharacterBustViewModelFactory characterBustViewModelFactory,
        IItemSlotViewModelFactory itemSlotViewModelFactory)
    {
        _dataManager = dataManager;
        _gauges = new PlayerGaugesViewModel();
        _statusEffects = new PlayerStatusEffectsViewModel();
        _conditions = new PlayerConditionsViewModel();
        _attributes = new PlayerAttributesViewModel();
        _position = new PlayerPositionViewModel(dataManager);
        _inventory = new InventoryViewModel(player, dataManager, itemSlotViewModelFactory);
        _playerBustViewModel = characterBustViewModelFactory.Create();

        Update(player);
    }

    public void Update(DecodedInGamePlayer player)
    {
        UpdateProperties(player);
    }

    private void UpdateProperties(DecodedInGamePlayer player)
    {
        UniqueNameId = player.NameId > 0
            ? $"NameId_{player.NameId}"
            : $"Ulid_{player.Id}";

        CharacterName = player.Name;
        NameId = player.NameId;
        CharacterType = player.Type;
        IsEnabled = player.IsEnabled;
        IsInGame = player.IsInGame;

        Gauges.Update(player.CurHealth, player.MaxHealth, player.HealthPercentage, player.CurVirus, player.MaxVirus, player.VirusPercentage);
        StatusEffects.Update(player.BleedTime, player.AntiVirusTime, player.AntiVirusGTime, player.HerbTime, player.Status, _dataManager.InGameScenario.CurrentFile);
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

        if (EnumUtility.TryParseByValueOrMember(CharacterType, out CharacterBaseType charType))
            _ = PlayerBustViewModel.UpdateBustAsync(charType);
    }

    public override bool Equals(object? obj)
    {
        if (obj is InGamePlayerViewModel other)
            return string.Equals(UniqueNameId, other.UniqueNameId, System.StringComparison.Ordinal);

        return false;
    }

    public override int GetHashCode()
        => System.StringComparer.Ordinal.GetHashCode(UniqueNameId);
}