using OutbreakTracker2.Application.Services.Reports.Events;
using R3;

namespace OutbreakTracker2.Application.Services.Reports;

public interface IRunReportService : IDisposable
{
    bool IsRunning { get; }
    Observable<RunEvent> Events { get; }
    Observable<RunReport> CompletedReports { get; }
}
