using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.PCSX2.Client;

namespace OutbreakTracker2.Application.Services.Capabilities;

public interface IMemoryBackendCapabilityService
{
    MemoryBackendCapabilityReport Inspect(IGameClient? gameClient, OutbreakTrackerSettings? settingsOverride = null);
}
