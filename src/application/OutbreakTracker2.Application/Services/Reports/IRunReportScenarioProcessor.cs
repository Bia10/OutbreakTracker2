using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Reports;

internal interface IRunReportScenarioProcessor
{
    void Process(DecodedInGameScenario scenario, RunReportProcessingContext context);
}
