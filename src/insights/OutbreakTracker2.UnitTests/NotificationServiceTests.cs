using Microsoft.Extensions.Logging.Abstractions;
using OutbreakTracker2.Application.Services.Notifications;
using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.Application.Services.Toasts;
using OutbreakTracker2.Application.Services.Tracking;
using OutbreakTracker2.Outbreak.Models;
using R3;
using SukiUI.Toasts;

namespace OutbreakTracker2.UnitTests;

public sealed class NotificationServiceTests
{
    [Test]
    public async Task AlertSubscription_ContinuesAfterSynchronousToastFailure()
    {
        using FakeTrackerRegistry trackerRegistry = new();
        using FakeAppSettingsService settingsService = new(enableToastAlerts: true);
        FlakyToastService toastService = new();
        using NotificationService service = new(
            toastService,
            trackerRegistry,
            settingsService,
            NullLogger<NotificationService>.Instance
        );

        trackerRegistry.Emit(new AlertNotification("First", "Boom", AlertLevel.Info));
        trackerRegistry.Emit(new AlertNotification("Second", "Still works", AlertLevel.Info));

        await Assert.That(toastService.CallCount).IsEqualTo(2);
        await Assert.That(toastService.SuccessfulCallCount).IsEqualTo(1);
        await Assert.That(toastService.LastTitle).IsEqualTo("Second");
    }

    private sealed class FakeTrackerRegistry : ITrackerRegistry, IDisposable
    {
        private readonly Subject<AlertNotification> _alerts = new();

        public IReadOnlyEntityTracker<DecodedEnemy> Enemies => null!;

        public IReadOnlyEntityTracker<DecodedDoor> Doors => null!;

        public IReadOnlyEntityTracker<DecodedInGamePlayer> Players => null!;

        public IReadOnlyEntityTracker<DecodedLobbySlot> LobbySlots => null!;

        public IEntityChangeSource<DecodedEnemy> EnemyChanges => null!;

        public IEntityChangeSource<DecodedDoor> DoorChanges => null!;

        public IEntityChangeSource<DecodedInGamePlayer> PlayerChanges => null!;

        public IEntityChangeSource<DecodedLobbySlot> LobbySlotChanges => null!;

        public Observable<AlertNotification> AllAlerts => _alerts;

        public void Emit(AlertNotification alert) => _alerts.OnNext(alert);

        public void Dispose() => _alerts.Dispose();
    }

    private sealed class FakeAppSettingsService : IAppSettingsService
    {
        private readonly ReactiveProperty<OutbreakTrackerSettings> _settings;

        public FakeAppSettingsService(bool enableToastAlerts)
        {
            _settings = new ReactiveProperty<OutbreakTrackerSettings>(
                new OutbreakTrackerSettings
                {
                    Notifications = new NotificationSettings { EnableToastAlerts = enableToastAlerts },
                }
            );
        }

        public string UserSettingsPath => string.Empty;

        public OutbreakTrackerSettings Current => _settings.Value;

        public Observable<OutbreakTrackerSettings> SettingsObservable => _settings;

        public ValueTask SaveAsync(OutbreakTrackerSettings settings, CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;

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

    private sealed class FlakyToastService : IToastService
    {
        private bool _throwOnNextCall = true;

        public int CallCount { get; private set; }

        public int SuccessfulCallCount { get; private set; }

        public string LastTitle { get; private set; } = string.Empty;

        public Task InvokeInfoToastAsync(string content, string? title = "") => InvokeToastAsync(title);

        public Task InvokeSuccessToastAsync(string content, string? title = "") => InvokeToastAsync(title);

        public Task InvokeErrorToastAsync(string content, string? title = "") => InvokeToastAsync(title);

        public Task InvokeWarningToastAsync(string content, string? title = "") => InvokeToastAsync(title);

        public ISukiToast CreateToast(string title, object content) => throw new NotSupportedException();

        public ISukiToast CreateInfoToastWithCancelButton(
            string content,
            object cancelButtonContent,
            Action<ISukiToast> onCanceledAction,
            string? title = ""
        ) => throw new NotSupportedException();

        public Task DismissToastAsync(ISukiToast toast) => Task.CompletedTask;

        private Task InvokeToastAsync(string? title)
        {
            CallCount++;

            if (_throwOnNextCall)
            {
                _throwOnNextCall = false;
                throw new InvalidOperationException("Simulated toast dispatch failure.");
            }

            SuccessfulCallCount++;
            LastTitle = title ?? string.Empty;
            return Task.CompletedTask;
        }
    }
}
