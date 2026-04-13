using Microsoft.Extensions.Logging.Abstractions;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Dispatcher;
using OutbreakTracker2.Application.Views.Map.Canvas;
using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.UnitTests;

public sealed class MapCanvasViewModelTests
{
    [Test]
    public async Task InGamePlayersSubscription_ResumesAfterDispatcherFailure()
    {
        using FakeDataSource dataSource = new();
        FlakyDispatcherService dispatcherService = new();
        using MapCanvasViewModel viewModel = new(
            dataSource,
            dispatcherService,
            NullLogger<MapCanvasViewModel>.Instance
        );

        dataSource.SetInGamePlayers([]);

        bool failureObserved = SpinWait.SpinUntil(
            () => dispatcherService.PostFailureCount == 1,
            TimeSpan.FromSeconds(2)
        );
        await Assert.That(failureObserved).IsTrue();

        dataSource.SetInGamePlayers([new DecodedInGamePlayer { IsEnabled = true, IsInGame = true }]);

        bool resumed = SpinWait.SpinUntil(() => viewModel.IsInGame, TimeSpan.FromSeconds(2));
        await Assert.That(resumed).IsTrue();
    }

    private sealed class FakeDataSource : IDataObservableSource, IDisposable
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

    private sealed class FlakyDispatcherService : IDispatcherService
    {
        private int _remainingPostFailures = 1;

        public int PostFailureCount { get; private set; }

        public bool CheckAccess() => true;

        public bool IsOnUIThread() => true;

        public void PostOnUI(Action action)
        {
            if (Interlocked.Decrement(ref _remainingPostFailures) >= 0)
            {
                PostFailureCount++;
                throw new InvalidOperationException("Simulated dispatcher failure.");
            }

            action();
        }

        public Task InvokeOnUIAsync(Action action, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            action();
            return Task.CompletedTask;
        }

        public Task<TResult?> InvokeOnUIAsync<TResult>(
            Func<TResult> action,
            CancellationToken cancellationToken = default
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<TResult?>(action());
        }
    }
}
