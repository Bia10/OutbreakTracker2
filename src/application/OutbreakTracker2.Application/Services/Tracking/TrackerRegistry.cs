using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Services.Tracking;

public sealed class TrackerRegistry : ITrackerRegistry, IDisposable
{
    private readonly IEntityTracker<DecodedEnemy> _enemyTracker;
    private readonly IEntityTracker<DecodedDoor> _doorTracker;
    private readonly IEntityTracker<DecodedInGamePlayer> _playerTracker;
    private readonly IEntityTracker<DecodedLobbySlot> _lobbySlotTracker;

    public IReadOnlyEntityTracker<DecodedEnemy> Enemies => _enemyTracker;
    public IReadOnlyEntityTracker<DecodedDoor> Doors => _doorTracker;
    public IReadOnlyEntityTracker<DecodedInGamePlayer> Players => _playerTracker;
    public IReadOnlyEntityTracker<DecodedLobbySlot> LobbySlots => _lobbySlotTracker;

    public Observable<AlertNotification> AllAlerts { get; }

    public TrackerRegistry(
        IDataObservableSource dataObservable,
        IEntityTrackerFactory trackerFactory,
        IEnumerable<IAlertRuleProvider<DecodedEnemy>> enemyRuleProviders,
        IEnumerable<IAlertRuleProvider<DecodedDoor>> doorRuleProviders,
        IEnumerable<IAlertRuleProvider<DecodedInGamePlayer>> playerRuleProviders,
        IEnumerable<IAlertRuleProvider<DecodedLobbySlot>> lobbySlotRuleProviders
    )
    {
        _enemyTracker = trackerFactory.Create(dataObservable.EnemiesObservable);
        _doorTracker = trackerFactory.Create(dataObservable.DoorsObservable);
        _playerTracker = trackerFactory.Create(dataObservable.InGamePlayersObservable);
        _lobbySlotTracker = trackerFactory.Create(dataObservable.LobbySlotsObservable);

        Observable<AlertNotification> lobbySlotAlerts = _lobbySlotTracker.Alerts;
        Observable<AlertNotification> gatedLobbyAlerts = lobbySlotAlerts
            .WithLatestFrom(
                dataObservable.IsAtLobbyObservable,
                static (alert, isAtLobby) => (Alert: alert, IsAtLobby: isAtLobby)
            )
            .Where(static state => state.IsAtLobby)
            .Select(static state => state.Alert);

        RegisterProviders(_enemyTracker, enemyRuleProviders);
        RegisterProviders(_doorTracker, doorRuleProviders);
        RegisterProviders(_playerTracker, playerRuleProviders);
        RegisterProviders(_lobbySlotTracker, lobbySlotRuleProviders);

        AllAlerts = Observable.Merge(
            _enemyTracker.Alerts,
            _doorTracker.Alerts,
            _playerTracker.Alerts,
            gatedLobbyAlerts
        );
    }

    private static void RegisterProviders<T>(
        IEntityTracker<T> tracker,
        IEnumerable<IAlertRuleProvider<T>> ruleProviders
    )
        where T : IHasId
    {
        foreach (IAlertRuleProvider<T> ruleProvider in ruleProviders)
            ruleProvider.Register(tracker);
    }

    public void Dispose()
    {
        _enemyTracker.Dispose();
        _doorTracker.Dispose();
        _playerTracker.Dispose();
        _lobbySlotTracker.Dispose();
    }
}
