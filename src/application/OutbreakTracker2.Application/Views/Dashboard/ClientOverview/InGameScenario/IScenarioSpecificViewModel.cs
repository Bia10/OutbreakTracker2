using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario;

internal interface IScenarioSpecificViewModel
{
    Scenario ScenarioType { get; }

    void Update(DecodedInGameScenario scenario);
}
