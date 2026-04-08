namespace OutbreakTracker2.Application.Services.Reports;

public interface IRunReportWriter
{
    Task WriteAsync(RunReport report, CancellationToken cancellationToken = default);
}
