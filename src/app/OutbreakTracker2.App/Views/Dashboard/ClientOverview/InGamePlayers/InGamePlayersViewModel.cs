using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.App.Services.Data;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGamePlayer;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Utility;
using R3;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using ZLinq;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGamePlayers;

public class InGamePlayersViewModel : ObservableObject, IDisposable
{
    private readonly IDisposable _subscription;
    private readonly Dictionary<byte, InGamePlayerViewModel> _viewModelCache = new();
    private readonly ILogger<InGamePlayersViewModel> _logger;
    private readonly IDataManager _dataManager;

    public ObservableCollection<InGamePlayerViewModel> Players { get; } = new();

    public InGamePlayersViewModel(IDataManager dataManager, ILogger<InGamePlayersViewModel> logger)
    {
        _dataManager = dataManager;
        _logger = logger;
        _logger.LogInformation("Initializing InGamePlayersViewModel");

        _subscription = _dataManager.InGamePlayersObservable
            .Select(players => players.ToList())
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(UpdatePlayers);
    }

    private void UpdatePlayers(List<DecodedInGamePlayer> updatedPlayers)
    {
        _logger.LogInformation("Updating players with {Count} unique entries", updatedPlayers.Count);

        var currentIds = new HashSet<byte>(_viewModelCache.Keys);
        var newIds = new HashSet<byte>(updatedPlayers.Select(p => p.NameId));
        foreach (byte id in currentIds.Where(id => !newIds.Contains(id)).ToList())
            if (_viewModelCache.Remove(id, out InGamePlayerViewModel? vm))
                Players.Remove(vm);

        int targetIndex = 0;
        foreach (DecodedInGamePlayer player in updatedPlayers.Where(player => !string.IsNullOrEmpty(player.CharacterName)))
        {
            _logger.LogInformation("Updating player {Name}", player.CharacterName);

            UpdateRoomName(player);
            UpdateInventoryNames(player);

            if (_viewModelCache.TryGetValue(player.NameId, out InGamePlayerViewModel? existingVm))
            {
                existingVm.Update(player);

                int currentIndex = Players.IndexOf(existingVm);
                if (currentIndex != targetIndex)
                {
                    if (currentIndex >= 0 && currentIndex < Players.Count && targetIndex >= 0 && targetIndex < Players.Count)
                        Players.Move(currentIndex, targetIndex);
                    else
                        _logger.LogError("Invalid move indices. Current: {Current}, Target: {Target}, Count: {Count}", currentIndex, targetIndex, Players.Count);
                }
            }
            else
            {
                var newVm = new InGamePlayerViewModel(player);
                _viewModelCache[player.NameId] = newVm;
                Players.Insert(targetIndex, newVm);
            }

            targetIndex++;
        }
    }

    private void UpdateRoomName(DecodedInGamePlayer player)
    {
        string curScenarioName = _dataManager.InGameScenario.ScenarioName;
        if (!string.IsNullOrEmpty(curScenarioName) && EnumUtility.TryParseByValueOrMember(curScenarioName, out InGameScenario scenarioEnum))
            player.RoomName = GetRoomName(scenarioEnum, player.RoomId);
    }

    private void UpdateInventoryNames(DecodedInGamePlayer player)
    {
        if (!player.Enabled) return;

        if (HasDeadInventory(player))
        {
            UpdateDeadInventory(player);
            if (HasSpecialInventory(player))
                UpdateSpecialDeadInventory(player);
        }

        if (HasSpecialInventory(player))
        {
            UpdateSpecialItem(player);
            UpdateSpecialInventory(player);
        }

        UpdateEquippedItem(player);
        UpdateInventory(player);
    }

    private static bool HasDeadInventory(DecodedInGamePlayer player)
        => player.Status is "Zombie" or "Dead";

    private static bool HasSpecialInventory(DecodedInGamePlayer player)
        => player.CharacterName is "Yoko" or "David" or "Cindy";

    private void UpdateInventoryInternal(byte[] sourceInventory, string[] targetNamedInventory)
    {
        if (_dataManager.InGameScenario?.Items is null)
        {
            Array.Fill(targetNamedInventory, "Scenario data not loaded");
            return;
        }

        for (int i = 0; i < sourceInventory.Length; i++)
        {
            byte itemId = sourceInventory[i];
            if (itemId is 0x0)
            {
                targetNamedInventory[i] = "Empty|[0x00](0)|";
                continue;
            }

            targetNamedInventory[i] = LookupItemName(itemId);
        }
    }

    private string LookupItemName(byte itemId)
    {
        string? itemName = _dataManager.InGameScenario.Items
            .AsValueEnumerable()
            .Where(item => item.Id.Equals(itemId))
            .Select(item => item.TypeName)
            .FirstOrDefault();

        return itemName is not null
            ? $"{itemName}|[0x{itemId:X2}]({itemId})|"
            : $"Unknown item|[0x{itemId:X2}]({itemId})|";
    }

    private void UpdateEquippedItem(DecodedInGamePlayer player)
        => player.EquippedItemNamed = LookupItemName(player.EquippedItem);

    private void UpdateSpecialItem(DecodedInGamePlayer player)
        => player.SpecialItemNamed = LookupItemName(player.SpecialItem);

    private void UpdateInventory(DecodedInGamePlayer player)
        => UpdateInventoryInternal(player.Inventory, player.InventoryNamed);

    private void UpdateSpecialInventory(DecodedInGamePlayer player)
        => UpdateInventoryInternal(player.SpecialInventory, player.SpecialInventoryNamed);

    private void UpdateDeadInventory(DecodedInGamePlayer player)
        => UpdateInventoryInternal(player.DeadInventory, player.DeadInventoryNamed);

    private void UpdateSpecialDeadInventory(DecodedInGamePlayer player)
        => UpdateInventoryInternal(player.SpecialDeadInventory, player.SpecialDeadInventoryNamed);

    // TODO: move elsewhere
    private static string GetRoomName(InGameScenario scenarioName, short roomID)
    {
        string result = scenarioName switch
        {
            InGameScenario.Unknown => EnumUtility.GetEnumString(roomID, TrainingGroundRooms.Spawning),
            InGameScenario.TrainingGround => EnumUtility.GetEnumString(roomID, TrainingGroundRooms.Spawning),
            InGameScenario.EndOfTheRoad => EnumUtility.GetEnumString(roomID, EndOfTheRoadRooms.Spawning),
            InGameScenario.Underbelly => EnumUtility.GetEnumString(roomID, UnderbellyRooms.Spawning),
            InGameScenario.DesperateTimes => EnumUtility.GetEnumString(roomID, DesperateTimesRooms.Spawning),
            InGameScenario.Showdown1 => EnumUtility.GetEnumString(roomID, ShowdownRooms.Spawning),
            InGameScenario.Showdown2 => EnumUtility.GetEnumString(roomID, ShowdownRooms.Spawning),
            InGameScenario.Showdown3 => EnumUtility.GetEnumString(roomID, ShowdownRooms.Spawning),
            InGameScenario.Flashback => EnumUtility.GetEnumString(roomID, FlashbackRooms.Spawning),
            InGameScenario.Elimination3 => EnumUtility.GetEnumString(roomID, Elimination3Rooms.Spawning),
            InGameScenario.Elimination1 => EnumUtility.GetEnumString(roomID, Elimination1Rooms.Spawning),
            InGameScenario.Elimination2 => EnumUtility.GetEnumString(roomID, Elimination2Rooms.Spawning),
            InGameScenario.WildThings => EnumUtility.GetEnumString(roomID, WildThingsRooms.Spawning),
            _ => throw new ArgumentOutOfRangeException(nameof(scenarioName), scenarioName, null)
        };

        return result;
    }

    public void Dispose()
    {
        _subscription.Dispose();
        _viewModelCache.Clear();
        Players.Clear();
        GC.SuppressFinalize(this);
    }
}