using Avalonia;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging.Abstractions;
using OutbreakTracker2.Application.Services.Atlas;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Dispatcher;
using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.Application.Views.Common;
using OutbreakTracker2.Application.Views.Common.Item;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.Entities;
using OutbreakTracker2.Application.Views.GameDock.Dockables;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using R3;
using SukiUI.Toasts;

namespace OutbreakTracker2.UnitTests;

public sealed class ScenarioItemsDockViewModelTests
{
    [Test]
    public async Task FiltersRoomGroupsToCurrentPlayerRoom_WhenSettingIsEnabled()
    {
        using FakeDataSource dataSource = new();
        using FakeAppSettingsService settingsService = new(
            new OutbreakTrackerSettings
            {
                Display = new DisplaySettings
                {
                    ScenarioItemsDock = new ScenarioItemsDockSettings { OnlyShowCurrentPlayerRoom = true },
                },
            }
        );
        using TestSynchronizationContextScope scope = new();
        using ScenarioEntitiesViewModel source = new(new NullToastService(), new StubItemImageViewModelFactory());

        dataSource.SetScenario(new DecodedInGameScenario { LocalPlayerSlotIndex = 0 });
        dataSource.SetInGamePlayers([CreatePlayer(slotIndex: 0, roomId: 1), CreatePlayer(slotIndex: 1, roomId: 3)]);

        source.UpdateItems(CreateItems(), IdentityProjection, frameCounter: 100, gameFile: GameFile.FileOne);

        using ScenarioItemsDockViewModel viewModel = new(
            source,
            dataSource,
            dataSource,
            settingsService,
            new ImmediateDispatcherService()
        );

        List<ScenarioRoomGroupViewModel> visibleGroups = ReadVisibleGroups(viewModel);

        await Assert.That(viewModel.HasRoomGroups).IsTrue();
        await Assert.That(visibleGroups.Count).IsEqualTo(1);
        await Assert.That(visibleGroups[0].RoomName).IsEqualTo("Room 1");
        await Assert.That(visibleGroups[0].Items.Count).IsEqualTo(1);
        await Assert.That(visibleGroups[0].Items[0].RoomId).IsEqualTo((byte)1);
    }

    [Test]
    public async Task RefiltersRoomGroups_WhenCurrentPlayerRoomChanges()
    {
        using FakeDataSource dataSource = new();
        using FakeAppSettingsService settingsService = new(
            new OutbreakTrackerSettings
            {
                Display = new DisplaySettings
                {
                    ScenarioItemsDock = new ScenarioItemsDockSettings { OnlyShowCurrentPlayerRoom = true },
                },
            }
        );
        using TestSynchronizationContextScope scope = new();
        using ScenarioEntitiesViewModel source = new(new NullToastService(), new StubItemImageViewModelFactory());

        dataSource.SetScenario(new DecodedInGameScenario { LocalPlayerSlotIndex = 0 });
        dataSource.SetInGamePlayers([CreatePlayer(slotIndex: 0, roomId: 1)]);

        source.UpdateItems(CreateItems(), IdentityProjection, frameCounter: 100, gameFile: GameFile.FileOne);

        using ScenarioItemsDockViewModel viewModel = new(
            source,
            dataSource,
            dataSource,
            settingsService,
            new ImmediateDispatcherService()
        );

        dataSource.SetInGamePlayers([CreatePlayer(slotIndex: 0, roomId: 3)]);

        List<ScenarioRoomGroupViewModel> visibleGroups = ReadVisibleGroups(viewModel);

        await Assert.That(visibleGroups.Count).IsEqualTo(1);
        await Assert.That(visibleGroups[0].RoomName).IsEqualTo("Room 3");
        await Assert.That(visibleGroups[0].Items[0].RoomId).IsEqualTo((byte)3);
    }

    [Test]
    public async Task ShowsAllRoomGroups_WhenSettingIsDisabledAtRuntime()
    {
        using FakeDataSource dataSource = new();
        using FakeAppSettingsService settingsService = new(
            new OutbreakTrackerSettings
            {
                Display = new DisplaySettings
                {
                    ScenarioItemsDock = new ScenarioItemsDockSettings { OnlyShowCurrentPlayerRoom = true },
                },
            }
        );
        using TestSynchronizationContextScope scope = new();
        using ScenarioEntitiesViewModel source = new(new NullToastService(), new StubItemImageViewModelFactory());

        dataSource.SetScenario(new DecodedInGameScenario { LocalPlayerSlotIndex = 0 });
        dataSource.SetInGamePlayers([CreatePlayer(slotIndex: 0, roomId: 1)]);

        source.UpdateItems(CreateItems(), IdentityProjection, frameCounter: 100, gameFile: GameFile.FileOne);

        using ScenarioItemsDockViewModel viewModel = new(
            source,
            dataSource,
            dataSource,
            settingsService,
            new ImmediateDispatcherService()
        );

        settingsService.SetCurrent(
            new OutbreakTrackerSettings
            {
                Display = new DisplaySettings
                {
                    ScenarioItemsDock = new ScenarioItemsDockSettings { OnlyShowCurrentPlayerRoom = false },
                },
            }
        );

        List<ScenarioRoomGroupViewModel> visibleGroups = ReadVisibleGroups(viewModel);

        await Assert.That(visibleGroups.Count).IsEqualTo(2);
    }

    [Test]
    public async Task MovesClearedSlotsToSpawningRoom_WhenSlotTurnsEmpty()
    {
        using FakeDataSource dataSource = new();
        using FakeAppSettingsService settingsService = new(
            new OutbreakTrackerSettings
            {
                Display = new DisplaySettings
                {
                    ScenarioItemsDock = new ScenarioItemsDockSettings { OnlyShowCurrentPlayerRoom = false },
                },
            }
        );
        using TestSynchronizationContextScope scope = new();
        using ScenarioEntitiesViewModel source = new(new NullToastService(), new StubItemImageViewModelFactory());

        dataSource.SetScenario(new DecodedInGameScenario { LocalPlayerSlotIndex = 0 });
        dataSource.SetInGamePlayers([CreatePlayer(slotIndex: 0, roomId: 1)]);

        source.UpdateItems(CreateItems(), IdentityProjection, frameCounter: 100, gameFile: GameFile.FileOne);

        using ScenarioItemsDockViewModel viewModel = new(
            source,
            dataSource,
            dataSource,
            settingsService,
            new ImmediateDispatcherService()
        );

        source.UpdateItems(
            CreateItemsWithClearedFirstSlot(),
            IdentityProjection,
            frameCounter: 101,
            gameFile: GameFile.FileOne
        );

        List<ScenarioRoomGroupViewModel> visibleGroups = ReadVisibleGroups(viewModel);

        await Assert.That(visibleGroups.Count).IsEqualTo(2);
        await Assert.That(visibleGroups[0].RoomName).IsEqualTo("Room 3");
        await Assert.That(visibleGroups[1].RoomName).IsEqualTo("Spawning/Scenario Cleared");
        await Assert.That(visibleGroups[1].Items.Count).IsEqualTo(1);
        await Assert.That(visibleGroups[1].Items[0].DisplayName).IsEqualTo("Empty");
        await Assert.That(visibleGroups[1].Items[0].RoomId).IsEqualTo((byte)0);
        await Assert.That(visibleGroups[1].Items[0].PositionIndex).IsEqualTo((byte)0);
    }

    [Test]
    public async Task RemovesClearedSlotsFromCurrentRoomFilter_WhenSlotTurnsEmpty()
    {
        using FakeDataSource dataSource = new();
        using FakeAppSettingsService settingsService = new(
            new OutbreakTrackerSettings
            {
                Display = new DisplaySettings
                {
                    ScenarioItemsDock = new ScenarioItemsDockSettings { OnlyShowCurrentPlayerRoom = true },
                },
            }
        );
        using TestSynchronizationContextScope scope = new();
        using ScenarioEntitiesViewModel source = new(new NullToastService(), new StubItemImageViewModelFactory());

        dataSource.SetScenario(new DecodedInGameScenario { LocalPlayerSlotIndex = 0 });
        dataSource.SetInGamePlayers([CreatePlayer(slotIndex: 0, roomId: 1)]);

        source.UpdateItems(CreateItems(), IdentityProjection, frameCounter: 100, gameFile: GameFile.FileOne);

        using ScenarioItemsDockViewModel viewModel = new(
            source,
            dataSource,
            dataSource,
            settingsService,
            new ImmediateDispatcherService()
        );

        source.UpdateItems(
            CreateItemsWithClearedFirstSlot(),
            IdentityProjection,
            frameCounter: 101,
            gameFile: GameFile.FileOne
        );

        List<ScenarioRoomGroupViewModel> visibleGroups = ReadVisibleGroups(viewModel);

        await Assert.That(viewModel.HasRoomGroups).IsFalse();
        await Assert.That(viewModel.EmptyMessage).IsEqualTo("No items in your room");
        await Assert.That(visibleGroups.Count).IsEqualTo(0);
    }

    [Test]
    public async Task DisablesTracking_WhenTrackedItemPickedUpWithoutExchange()
    {
        using TestSynchronizationContextScope scope = new();
        using ScenarioEntitiesViewModel source = new(new NullToastService(), new StubItemImageViewModelFactory());

        source.UpdateItems(CreateItems(), IdentityProjection, frameCounter: 100, gameFile: GameFile.FileOne);

        ScenarioItemSlotViewModel trackedSlot = source.Items[0];
        trackedSlot.IsPickupTracked = true;

        await Assert.That(trackedSlot.IsPickupTracked).IsTrue();

        source.UpdateItems(
            CreateItemsWithPickedUpFirstSlot(),
            IdentityProjection,
            frameCounter: 101,
            gameFile: GameFile.FileOne
        );

        await Assert.That(trackedSlot.IsPickupTracked).IsFalse();
        await Assert.That(trackedSlot.DisplayName).IsEqualTo("Empty");
    }

    [Test]
    public async Task MovesPickedUpSlotToSpawningRoom_WhenItemLootedWithoutExchange()
    {
        using FakeDataSource dataSource = new();
        using FakeAppSettingsService settingsService = new(
            new OutbreakTrackerSettings
            {
                Display = new DisplaySettings
                {
                    ScenarioItemsDock = new ScenarioItemsDockSettings { OnlyShowCurrentPlayerRoom = false },
                },
            }
        );
        using TestSynchronizationContextScope scope = new();
        using ScenarioEntitiesViewModel source = new(new NullToastService(), new StubItemImageViewModelFactory());

        dataSource.SetScenario(new DecodedInGameScenario { LocalPlayerSlotIndex = 0 });
        dataSource.SetInGamePlayers([CreatePlayer(slotIndex: 0, roomId: 1)]);

        source.UpdateItems(CreateItems(), IdentityProjection, frameCounter: 100, gameFile: GameFile.FileOne);

        using ScenarioItemsDockViewModel viewModel = new(
            source,
            dataSource,
            dataSource,
            settingsService,
            new ImmediateDispatcherService()
        );

        source.UpdateItems(
            CreateItemsWithPickedUpFirstSlot(),
            IdentityProjection,
            frameCounter: 101,
            gameFile: GameFile.FileOne
        );

        List<ScenarioRoomGroupViewModel> visibleGroups = ReadVisibleGroups(viewModel);

        await Assert.That(visibleGroups.Count).IsEqualTo(2);
        await Assert.That(visibleGroups[0].RoomName).IsEqualTo("Room 3");
        await Assert.That(visibleGroups[1].RoomName).IsEqualTo("Spawning/Scenario Cleared");
        await Assert.That(visibleGroups[1].Items.Count).IsEqualTo(1);
        await Assert.That(visibleGroups[1].Items[0].DisplayName).IsEqualTo("Empty");
        await Assert.That(visibleGroups[1].Items[0].RoomId).IsEqualTo((byte)0);
    }

    private static DecodedItem[] CreateItems() =>
        [
            new DecodedItem
            {
                SlotIndex = 1,
                Id = 1,
                TypeId = 10,
                TypeName = "Green Herb",
                Quantity = 1,
                Present = 1,
                RoomId = 1,
                RoomName = "Room 1",
            },
            new DecodedItem
            {
                SlotIndex = 2,
                Id = 2,
                TypeId = 11,
                TypeName = "Handgun Ammo",
                Quantity = 15,
                Present = 1,
                RoomId = 3,
                RoomName = "Room 3",
            },
        ];

    private static DecodedItem[] CreateItemsWithClearedFirstSlot() =>
        [
            new DecodedItem
            {
                SlotIndex = 1,
                Id = 1,
                TypeId = 0,
                TypeName = "Handgun",
                Quantity = 0,
                PickedUp = 0,
                Present = 0,
                RoomId = 1,
                RoomName = "Room 1",
            },
            new DecodedItem
            {
                SlotIndex = 2,
                Id = 2,
                TypeId = 11,
                TypeName = "Handgun Ammo",
                Quantity = 15,
                RoomId = 3,
                RoomName = "Room 3",
            },
        ];

    private static DecodedItem[] CreateItemsWithPickedUpFirstSlot() =>
        [
            new DecodedItem
            {
                SlotIndex = 1,
                Id = 1,
                TypeId = 10,
                TypeName = "Green Herb",
                Quantity = 1,
                PickedUp = 1,
                Present = 1,
                RoomId = 1,
                RoomName = "Room 1",
            },
            new DecodedItem
            {
                SlotIndex = 2,
                Id = 2,
                TypeId = 11,
                TypeName = "Handgun Ammo",
                Quantity = 15,
                Present = 1,
                RoomId = 3,
                RoomName = "Room 3",
            },
        ];

    private static DecodedInGamePlayer CreatePlayer(int slotIndex, short roomId) =>
        new()
        {
            Id = Ulid.NewUlid(),
            IsEnabled = true,
            IsInGame = true,
            SlotIndex = slotIndex,
            NameId = 1,
            Name = "Kevin",
            Type = "Kevin",
            RoomId = roomId,
        };

    private static DecodedItem IdentityProjection(DecodedItem item) => item;

    private static List<ScenarioRoomGroupViewModel> ReadVisibleGroups(ScenarioItemsDockViewModel viewModel)
    {
        List<ScenarioRoomGroupViewModel> visibleGroups = [];

        foreach (ScenarioRoomGroupViewModel roomGroup in viewModel.RoomGroups)
            visibleGroups.Add(roomGroup);

        return visibleGroups;
    }

    private sealed class FakeDataSource : IDataObservableSource, IDataSnapshot, IDisposable
    {
        private readonly ReactiveProperty<DecodedDoor[]> _doors = new([]);
        private readonly ReactiveProperty<DecodedEnemy[]> _enemies = new([]);
        private readonly ReactiveProperty<DecodedInGamePlayer[]> _inGamePlayers = new([]);
        private readonly ReactiveProperty<InGameOverviewSnapshot> _inGameOverview = new(new InGameOverviewSnapshot());
        private readonly ReactiveProperty<DecodedInGameScenario> _inGameScenario = new(new DecodedInGameScenario());
        private readonly ReactiveProperty<DecodedLobbyRoom> _lobbyRoom = new(new DecodedLobbyRoom());
        private readonly ReactiveProperty<DecodedLobbyRoomPlayer[]> _lobbyRoomPlayers = new([]);
        private readonly ReactiveProperty<DecodedLobbySlot[]> _lobbySlots = new([]);
        private readonly ReactiveProperty<bool> _isAtLobby = new(false);

        public DecodedDoor[] Doors => _doors.Value;
        public DecodedEnemy[] Enemies => _enemies.Value;
        public DecodedInGamePlayer[] InGamePlayers => _inGamePlayers.Value;
        public DecodedInGameScenario InGameScenario => _inGameScenario.Value;
        public DecodedLobbyRoom LobbyRoom => _lobbyRoom.Value;
        public DecodedLobbyRoomPlayer[] LobbyRoomPlayers => _lobbyRoomPlayers.Value;
        public DecodedLobbySlot[] LobbySlots => _lobbySlots.Value;
        public bool IsAtLobby => _isAtLobby.Value;

        public Observable<DecodedDoor[]> DoorsObservable => _doors;
        public Observable<DecodedEnemy[]> EnemiesObservable => _enemies;
        public Observable<DecodedInGamePlayer[]> InGamePlayersObservable => _inGamePlayers;
        public Observable<InGameOverviewSnapshot> InGameOverviewObservable => _inGameOverview;
        public Observable<DecodedInGameScenario> InGameScenarioObservable => _inGameScenario;
        public Observable<DecodedLobbyRoom> LobbyRoomObservable => _lobbyRoom;
        public Observable<DecodedLobbyRoomPlayer[]> LobbyRoomPlayersObservable => _lobbyRoomPlayers;
        public Observable<DecodedLobbySlot[]> LobbySlotsObservable => _lobbySlots;
        public Observable<bool> IsAtLobbyObservable => _isAtLobby;

        public void SetInGamePlayers(DecodedInGamePlayer[] players) => _inGamePlayers.Value = players;

        public void SetScenario(DecodedInGameScenario scenario) => _inGameScenario.Value = scenario;

        public void Dispose()
        {
            _doors.Dispose();
            _enemies.Dispose();
            _inGamePlayers.Dispose();
            _inGameOverview.Dispose();
            _inGameScenario.Dispose();
            _lobbyRoom.Dispose();
            _lobbyRoomPlayers.Dispose();
            _lobbySlots.Dispose();
            _isAtLobby.Dispose();
        }
    }

    private sealed class FakeAppSettingsService : IAppSettingsService
    {
        private readonly ReactiveProperty<OutbreakTrackerSettings> _settings;

        public FakeAppSettingsService(OutbreakTrackerSettings settings)
        {
            _settings = new ReactiveProperty<OutbreakTrackerSettings>(settings);
        }

        public string UserSettingsPath { get; } =
            Path.Combine(Path.GetTempPath(), "outbreaktracker2-test-settings.json");

        public OutbreakTrackerSettings Current => _settings.Value;

        public Observable<OutbreakTrackerSettings> SettingsObservable => _settings;

        public void SetCurrent(OutbreakTrackerSettings settings) => _settings.Value = settings;

        public ValueTask SaveAsync(OutbreakTrackerSettings settings, CancellationToken cancellationToken = default)
        {
            _settings.Value = settings;
            return ValueTask.CompletedTask;
        }

        public ValueTask ExportAsync(Stream destination, CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;

        public ValueTask<OutbreakTrackerSettings> ImportAsync(
            Stream source,
            CancellationToken cancellationToken = default
        ) => ValueTask.FromResult(Current);

        public ValueTask<OutbreakTrackerSettings> ResetToDefaultsAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Current);

        public void Dispose() => _settings.Dispose();
    }

    private sealed class TestSynchronizationContextScope : IDisposable
    {
        private readonly SynchronizationContext? _previousContext = SynchronizationContext.Current;

        public TestSynchronizationContextScope() =>
            SynchronizationContext.SetSynchronizationContext(new ImmediateSynchronizationContext());

        public void Dispose() => SynchronizationContext.SetSynchronizationContext(_previousContext);
    }

    private sealed class ImmediateSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state) => d(state);

        public override void Send(SendOrPostCallback d, object? state) => d(state);
    }

    private sealed class ImmediateDispatcherService : IDispatcherService
    {
        public bool IsOnUIThread() => true;

        public void PostOnUI(Action action) => action();

        public Task InvokeOnUIAsync(Action action, CancellationToken cancellationToken = default)
        {
            action();
            return Task.CompletedTask;
        }

        public Task<TResult?> InvokeOnUIAsync<TResult>(
            Func<TResult> action,
            CancellationToken cancellationToken = default
        )
        {
            TResult result = action();
            return Task.FromResult<TResult?>(result);
        }
    }

    private sealed class NullToastService : OutbreakTracker2.Application.Services.Toasts.IToastService
    {
        public Task InvokeSuccessToastAsync(string text, string? title = null) => Task.CompletedTask;

        public Task InvokeInfoToastAsync(string text, string? title = null) => Task.CompletedTask;

        public Task InvokeWarningToastAsync(string text, string? title = null) => Task.CompletedTask;

        public Task InvokeErrorToastAsync(string text, string? title = null) => Task.CompletedTask;

        public ISukiToast CreateToast(string title, object content)
        {
            throw new NotImplementedException();
        }

        public ISukiToast CreateInfoToastWithCancelButton(
            string content,
            object cancelButtonContent,
            Action<ISukiToast> onCanceledAction,
            string? title = ""
        )
        {
            throw new NotImplementedException();
        }

        public Task DismissToastAsync(ISukiToast toast)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class StubItemImageViewModelFactory : IItemImageViewModelFactory
    {
        public ItemImageViewModel Create() =>
            new(NullLogger<ItemImageViewModel>.Instance, new StubImageViewModelFactory());
    }

    private sealed class StubImageViewModelFactory : IImageViewModelFactory
    {
        public ImageViewModel Create() =>
            new(NullLogger<ImageViewModel>.Instance, new StubTextureAtlasService(), new ImmediateDispatcherService());
    }

    private sealed class StubTextureAtlasService : ITextureAtlasService
    {
        private static readonly ITextureAtlas EmptyAtlas = new StubTextureAtlas();

        public ITextureAtlas GetAtlas(string name) => EmptyAtlas;

        public IReadOnlyDictionary<string, ITextureAtlas> GetAllAtlases() => new Dictionary<string, ITextureAtlas>();

        public Task LoadAtlasesAsync() => Task.CompletedTask;
    }

    private sealed class StubTextureAtlas : ITextureAtlas
    {
        public Bitmap? Texture => null;

        public bool TryGetSourceRectangle(string name, out Rect rect)
        {
            rect = default;
            return false;
        }

        public Rect GetSourceRectangle(string name) => default;
    }
}
