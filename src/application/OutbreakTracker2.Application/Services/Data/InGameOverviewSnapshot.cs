using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Data;

public sealed record InGameOverviewSnapshot(
    DecodedInGameScenario Scenario,
    DecodedInGamePlayer[] Players,
    DecodedEnemy[] Enemies,
    DecodedDoor[] Doors
)
{
    public InGameOverviewSnapshot()
        : this(new DecodedInGameScenario(), [], [], []) { }
}
