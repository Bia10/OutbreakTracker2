using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Tracking;

internal sealed class PlayerAlertRulesProvider(IAppSettingsService settingsService, IDataSnapshot dataSnapshot)
    : IAlertRuleProvider<DecodedInGamePlayer>
{
    private readonly IAppSettingsService _settingsService = settingsService;
    private readonly IDataSnapshot _dataSnapshot = dataSnapshot;

    public void Register(IEntityTracker<DecodedInGamePlayer> tracker) =>
        DefaultPlayerAlertRules.Register(tracker, _settingsService, _dataSnapshot);
}
