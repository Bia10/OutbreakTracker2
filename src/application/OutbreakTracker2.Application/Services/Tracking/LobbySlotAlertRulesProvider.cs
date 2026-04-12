using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Tracking;

internal sealed class LobbySlotAlertRulesProvider(IAppSettingsService settingsService)
    : IAlertRuleProvider<DecodedLobbySlot>
{
    private readonly IAppSettingsService _settingsService = settingsService;

    public void Register(IEntityTracker<DecodedLobbySlot> tracker) =>
        DefaultLobbySlotAlertRules.Register(tracker, _settingsService);
}
