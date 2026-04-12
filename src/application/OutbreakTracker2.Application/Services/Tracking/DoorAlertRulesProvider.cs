using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Tracking;

internal sealed class DoorAlertRulesProvider(IAppSettingsService settingsService, IDataSnapshot dataSnapshot)
    : IAlertRuleProvider<DecodedDoor>
{
    private readonly IAppSettingsService _settingsService = settingsService;
    private readonly IDataSnapshot _dataSnapshot = dataSnapshot;

    public void Register(IEntityTracker<DecodedDoor> tracker) =>
        DefaultDoorAlertRules.Register(tracker, _settingsService, _dataSnapshot);
}
