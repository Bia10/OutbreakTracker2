using R3;

namespace OutbreakTracker2.Application.Services.Settings;

public interface IAppSettingsService : IDisposable
{
    string UserSettingsPath { get; }

    OutbreakTrackerSettings Current { get; }

    Observable<OutbreakTrackerSettings> SettingsObservable { get; }

    ValueTask SaveAsync(OutbreakTrackerSettings settings, CancellationToken cancellationToken = default);

    ValueTask ExportAsync(Stream destination, CancellationToken cancellationToken = default);

    ValueTask<OutbreakTrackerSettings> ImportAsync(Stream source, CancellationToken cancellationToken = default);

    ValueTask<OutbreakTrackerSettings> ResetToDefaultsAsync(CancellationToken cancellationToken = default);
}
