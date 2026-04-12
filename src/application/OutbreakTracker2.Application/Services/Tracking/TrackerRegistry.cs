using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Services.Tracking;

public sealed class TrackerRegistry : ITrackerRegistry, IDisposable
{
    public IEntityTracker<DecodedEnemy> Enemies { get; }
    public IEntityTracker<DecodedDoor> Doors { get; }
    public IEntityTracker<DecodedInGamePlayer> Players { get; }
    public IEntityTracker<DecodedLobbySlot> LobbySlots { get; }

    public Observable<AlertNotification> AllAlerts { get; }

    internal TrackerRegistry(
        IDataObservableSource dataObservable,
        IDataSnapshot dataSnapshot,
        ICurrentScenarioState scenarioState,
        IAppSettingsService settingsService,
        IEntityTrackerFactory trackerFactory
    )
        : this(
            dataObservable,
            trackerFactory,
            [
                new DelegateAlertRuleProvider<DecodedEnemy>(tracker =>
                    EnemyAlertRules.Register(tracker, settingsService, scenarioState)
                ),
            ],
            [
                new DelegateAlertRuleProvider<DecodedDoor>(tracker =>
                    DefaultDoorAlertRules.Register(tracker, settingsService, dataSnapshot)
                ),
            ],
            [
                new DelegateAlertRuleProvider<DecodedInGamePlayer>(tracker =>
                    DefaultPlayerAlertRules.Register(tracker, settingsService, scenarioState)
                ),
            ],
            [
                new DelegateAlertRuleProvider<DecodedLobbySlot>(tracker =>
                    DefaultLobbySlotAlertRules.Register(tracker, settingsService)
                ),
            ]
        ) { }

    public TrackerRegistry(
        IDataObservableSource dataObservable,
        IEntityTrackerFactory trackerFactory,
        IEnumerable<IAlertRuleProvider<DecodedEnemy>> enemyRuleProviders,
        IEnumerable<IAlertRuleProvider<DecodedDoor>> doorRuleProviders,
        IEnumerable<IAlertRuleProvider<DecodedInGamePlayer>> playerRuleProviders,
        IEnumerable<IAlertRuleProvider<DecodedLobbySlot>> lobbySlotRuleProviders
    )
    {
        Enemies = trackerFactory.Create(dataObservable.EnemiesObservable);
        Doors = trackerFactory.Create(dataObservable.DoorsObservable);
        Players = trackerFactory.Create(dataObservable.InGamePlayersObservable);
        LobbySlots = trackerFactory.Create(dataObservable.LobbySlotsObservable);

        Observable<AlertNotification> lobbySlotAlerts = LobbySlots.Alerts;
        Observable<AlertNotification> gatedLobbyAlerts = lobbySlotAlerts
            .WithLatestFrom(
                dataObservable.IsAtLobbyObservable,
                static (alert, isAtLobby) => (Alert: alert, IsAtLobby: isAtLobby)
            )
            .Where(static state => state.IsAtLobby)
            .Select(static state => state.Alert);

        RegisterProviders(Enemies, enemyRuleProviders);
        RegisterProviders(Doors, doorRuleProviders);
        RegisterProviders(Players, playerRuleProviders);
        RegisterProviders(LobbySlots, lobbySlotRuleProviders);

        AllAlerts = Observable.Merge(Enemies.Alerts, Doors.Alerts, Players.Alerts, gatedLobbyAlerts);
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
        Enemies.Dispose();
        Doors.Dispose();
        Players.Dispose();
        LobbySlots.Dispose();
    }
}
