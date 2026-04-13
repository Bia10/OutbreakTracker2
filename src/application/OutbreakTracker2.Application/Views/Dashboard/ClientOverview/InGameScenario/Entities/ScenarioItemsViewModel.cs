using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Dispatcher;
using OutbreakTracker2.Application.Services.Toasts;
using OutbreakTracker2.Application.Views.Common.Item;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Utility;
using R3;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.Entities;

public sealed partial class ScenarioItemsViewModel : ObservableObject, IDisposable
{
    private const string ClearedRoomName = "Spawning/Scenario Cleared";

    private readonly ILogger<ScenarioItemsViewModel> _logger;
    private readonly IToastService _toastService;
    private readonly IItemImageViewModelFactory _itemImageViewModelFactory;
    private readonly Dictionary<short, InvalidPickedUpWarning> _lastInvalidPickedUpWarnings = [];
    private readonly Dictionary<byte, short> _previousPickedUpStates = [];
    private readonly HashSet<int> _visibleItemIndices = [];
    private readonly Dictionary<string, List<ScenarioItemSlotViewModel>> _roomItemsByName = new(StringComparer.Ordinal);
    private readonly List<ScenarioRoomGroupViewModel> _roomGroupBuffer = [];
    private readonly ObservableList<ScenarioItemSlotViewModel> _items = [];
    private readonly ObservableList<ScenarioRoomGroupViewModel> _roomGroups = [];
    private DisposableBag _disposables;

    public NotifyCollectionChangedSynchronizedViewList<ScenarioItemSlotViewModel> Items { get; }
    public NotifyCollectionChangedSynchronizedViewList<ScenarioRoomGroupViewModel> RoomGroups { get; }

    [ObservableProperty]
    private bool _hasRoomGroups;

    public ScenarioItemsViewModel(
        ILogger<ScenarioItemsViewModel> logger,
        IDataObservableSource dataObservable,
        IDispatcherService dispatcherService,
        IToastService toastService,
        IItemImageViewModelFactory itemImageViewModelFactory
    )
    {
        _logger = logger;
        _toastService = toastService;
        _itemImageViewModelFactory = itemImageViewModelFactory;

        Items = _items.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
        RoomGroups = _roomGroups.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

        _disposables.Add(
            dataObservable
                .InGameOverviewObservable.ObserveOnThreadPool()
                .SubscribeAwait(
                    async (snapshot, cancellationToken) =>
                    {
                        try
                        {
                            await dispatcherService
                                .InvokeOnUIAsync(() => ApplySnapshot(snapshot), cancellationToken)
                                .ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogInformation("Scenario item update processing cancelled");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error during scenario item update processing cycle");
                        }
                    },
                    AwaitOperation.Drop
                )
        );
    }

    public void Dispose()
    {
        _disposables.Dispose();
        ClearItems();
        Items.Dispose();
        RoomGroups.Dispose();
    }

    public void ClearItems()
    {
        DisposeItemViewModels();
        _items.Clear();
        _roomGroups.Clear();
        _lastInvalidPickedUpWarnings.Clear();
        _previousPickedUpStates.Clear();
        _visibleItemIndices.Clear();
        _roomItemsByName.Clear();
        _roomGroupBuffer.Clear();
        HasRoomGroups = false;
    }

    public void UpdateItems(
        DecodedItem[] newItems,
        Func<DecodedItem, DecodedItem> projectDisplayItem,
        int frameCounter,
        GameFile gameFile
    )
    {
        ArgumentNullException.ThrowIfNull(newItems);
        ArgumentNullException.ThrowIfNull(projectDisplayItem);

        if (_items.Count == 0)
        {
            ScenarioItemSlotViewModel[] viewModels = new ScenarioItemSlotViewModel[newItems.Length];
            for (int i = 0; i < newItems.Length; i++)
            {
                DecodedItem displayItem = NormalizeDisplayItem(projectDisplayItem(newItems[i]));
                ItemImageViewModel imageVm = _itemImageViewModelFactory.Create();
                viewModels[i] = new ScenarioItemSlotViewModel(displayItem, imageVm, gameFile, (byte)i);
                _previousPickedUpStates[(byte)i] = newItems[i].PickedUp;
            }

            _items.AddRange(viewModels);
            RebuildRoomGroups();
            return;
        }

        int itemCount = Math.Min(newItems.Length, _items.Count);
        bool anyItemChanged = false;

        for (int i = 0; i < itemCount; i++)
        {
            DecodedItem newItem = newItems[i];
            DecodedItem projectedItem = projectDisplayItem(newItem);
            DecodedItem displayItem = NormalizeDisplayItem(projectedItem);
            ScenarioItemSlotViewModel viewModel = _items[i];
            byte slotKey = (byte)i;
            string trackedTypeName = string.IsNullOrEmpty(viewModel.TypeName) ? newItem.TypeName : viewModel.TypeName;

            if (!anyItemChanged && viewModel.Item != displayItem)
                anyItemChanged = true;

            if (viewModel.IsPickupTracked && !string.IsNullOrEmpty(newItem.TypeName))
            {
                short previousPickedUp = _previousPickedUpStates.GetValueOrDefault(slotKey, (short)0);
                if (previousPickedUp == 0 && newItem.PickedUp > 0)
                {
                    string holder =
                        string.IsNullOrEmpty(projectedItem.PickedUpByName)
                        || string.Equals(projectedItem.PickedUpByName, "None", StringComparison.Ordinal)
                            ? $"P{newItem.PickedUp}"
                            : projectedItem.PickedUpByName;
                    _ = _toastService.InvokeInfoToastAsync($"{holder} picked up {trackedTypeName}", "Item Picked Up");
                    viewModel.IsPickupTracked = false;
                }
            }

            _previousPickedUpStates[slotKey] = newItem.PickedUp;
            viewModel.UpdateItem(displayItem, frameCounter, gameFile, slotKey);
        }

        if (anyItemChanged)
            RebuildRoomGroups();
    }

    private void ApplySnapshot(InGameOverviewSnapshot snapshot)
    {
        if (snapshot.Scenario.Status.IsTransitional())
            return;

        if (snapshot.Scenario.CurrentFile is < 1 or > 2)
        {
            ClearItems();
            return;
        }

        Scenario scenario = EnumUtility.TryParseByValueOrMember(
            snapshot.Scenario.ScenarioName,
            out Scenario parsedScenario
        )
            ? parsedScenario
            : Scenario.Unknown;

        UpdateItems(
            snapshot.Scenario.Items,
            item => ProjectDisplayItem(item, snapshot.Players, scenario),
            snapshot.Scenario.FrameCounter,
            (GameFile)snapshot.Scenario.CurrentFile
        );
    }

    private DecodedItem ProjectDisplayItem(DecodedItem item, DecodedInGamePlayer[] players, Scenario scenario)
    {
        string roomName = scenario.GetRoomName(item.RoomId);
        string pickedUpByName = ResolvePickedUpByName(item, players);

        return item with
        {
            RoomName = roomName,
            PickedUpByName = pickedUpByName,
        };
    }

    private string ResolvePickedUpByName(in DecodedItem item, DecodedInGamePlayer[] players)
    {
        if (item.PickedUp == 0)
        {
            _lastInvalidPickedUpWarnings.Remove(item.Id);
            return "None";
        }

        int slotIndex = item.PickedUp - 1;
        if (slotIndex >= 0 && slotIndex < players.Length)
        {
            _lastInvalidPickedUpWarnings.Remove(item.Id);
            DecodedInGamePlayer player = players[slotIndex];
            return player.IsEnabled && !string.IsNullOrEmpty(player.Name) ? player.Name : $"P{item.PickedUp}";
        }

        if (ShouldWarnInvalidPickedUp(item, players.Length))
        {
            InvalidPickedUpWarning warning = new(
                item.SlotIndex,
                item.TypeName,
                item.PickedUp,
                item.Present,
                item.Quantity,
                players.Length
            );

            if (
                !_lastInvalidPickedUpWarnings.TryGetValue(item.Id, out InvalidPickedUpWarning lastWarning)
                || lastWarning != warning
            )
            {
                _lastInvalidPickedUpWarnings[item.Id] = warning;
                _logger.LogWarning(
                    "Item slot={Slot} type={Type} has PickedUp={PickedUp} which is out of valid player range [1,{Max}]; Present={Present} Qty={Qty}",
                    item.SlotIndex,
                    item.TypeName,
                    item.PickedUp,
                    players.Length,
                    item.Present,
                    item.Quantity
                );
            }
        }
        else
        {
            _lastInvalidPickedUpWarnings.Remove(item.Id);
        }

        return $"P{item.PickedUp}";
    }

    private static bool ShouldWarnInvalidPickedUp(DecodedItem item, int playerCount) =>
        playerCount > 0 && item.Present != 0;

    private static DecodedItem NormalizeDisplayItem(in DecodedItem item)
    {
        bool isCleared = item is { TypeId: 0, Quantity: 0, PickedUp: 0, Present: 0 };
        bool isPickedUp = item.PickedUp > 0;

        if (!isCleared && !isPickedUp)
            return item;

        return item with
        {
            TypeId = 0,
            TypeName = string.Empty,
            Quantity = 0,
            PickedUp = 0,
            Present = 0,
            RoomId = 0,
            RoomName = ClearedRoomName,
            PickedUpByName = "None",
        };
    }

    private void RebuildRoomGroups()
    {
        _visibleItemIndices.Clear();
        foreach (int index in ScenarioItemRoomGroupProjection.GetVisibleIndices(_items))
            _visibleItemIndices.Add(index);

        _roomItemsByName.Clear();
        for (int i = 0; i < _items.Count; i++)
        {
            if (!_visibleItemIndices.Contains(i))
                continue;

            ScenarioItemSlotViewModel item = _items[i];
            string roomName = NormalizeRoomGroupName(item.RoomName);
            if (!_roomItemsByName.TryGetValue(roomName, out List<ScenarioItemSlotViewModel>? roomItems))
            {
                roomItems = [];
                _roomItemsByName.Add(roomName, roomItems);
            }

            roomItems.Add(item);
        }

        _roomGroupBuffer.Clear();
        foreach (KeyValuePair<string, List<ScenarioItemSlotViewModel>> pair in _roomItemsByName)
            _roomGroupBuffer.Add(new ScenarioRoomGroupViewModel(pair.Key, pair.Value));

        _roomGroupBuffer.Sort(
            static (left, right) =>
            {
                bool leftCleared = string.Equals(left.RoomName, ClearedRoomName, StringComparison.Ordinal);
                bool rightCleared = string.Equals(right.RoomName, ClearedRoomName, StringComparison.Ordinal);
                if (leftCleared != rightCleared)
                    return leftCleared ? 1 : -1;

                return StringComparer.Ordinal.Compare(left.RoomName, right.RoomName);
            }
        );

        ApplyRoomGroupChanges(_roomGroupBuffer);
        HasRoomGroups = _roomGroupBuffer.Count > 0;
    }

    private static string NormalizeRoomGroupName(string roomName) =>
        string.IsNullOrEmpty(roomName) ? "Unknown" : roomName;

    private void ApplyRoomGroupChanges(IReadOnlyList<ScenarioRoomGroupViewModel> newGroups)
    {
        while (_roomGroups.Count > newGroups.Count)
            _roomGroups.RemoveAt(_roomGroups.Count - 1);

        for (int i = 0; i < newGroups.Count; i++)
        {
            if (i < _roomGroups.Count)
            {
                if (!IsSameGroup(_roomGroups[i], newGroups[i]))
                    _roomGroups[i] = newGroups[i];
            }
            else
            {
                _roomGroups.Add(newGroups[i]);
            }
        }
    }

    private static bool IsSameGroup(ScenarioRoomGroupViewModel existing, ScenarioRoomGroupViewModel candidate)
    {
        if (!string.Equals(existing.RoomName, candidate.RoomName, StringComparison.Ordinal))
            return false;

        if (existing.Items.Count != candidate.Items.Count)
            return false;

        for (int index = 0; index < existing.Items.Count; index++)
        {
            if (!ReferenceEquals(existing.Items[index], candidate.Items[index]))
                return false;
        }

        return true;
    }

    private void DisposeItemViewModels()
    {
        foreach (ScenarioItemSlotViewModel item in _items)
            item.Dispose();
    }

    private readonly record struct InvalidPickedUpWarning(
        byte SlotIndex,
        string TypeName,
        short PickedUp,
        int Present,
        short Quantity,
        int MaxPlayers
    );
}
