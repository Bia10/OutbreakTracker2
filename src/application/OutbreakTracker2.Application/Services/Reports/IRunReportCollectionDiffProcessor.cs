using OutbreakTracker2.Application.Services.Tracking;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Reports;

internal interface IRunReportCollectionDiffProcessor<T>
    where T : IHasId
{
    void Process(CollectionDiff<T> diff, RunReportProcessingContext context);
}
