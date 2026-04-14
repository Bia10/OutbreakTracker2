using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Services.Settings;

namespace OutbreakTracker2.Application.Services.Reports;

public sealed class CompositeRunReportWriter : IRunReportWriter
{
    private readonly CsvRunReportWriter _csvWriter;
    private readonly HtmlRunReportWriter _htmlWriter;
    private readonly IAppSettingsService _appSettingsService;
    private readonly ILogger<CompositeRunReportWriter> _logger;
    private readonly MarkdownRunReportWriter _markdownWriter;

    public CompositeRunReportWriter(
        MarkdownRunReportWriter markdownWriter,
        HtmlRunReportWriter htmlWriter,
        CsvRunReportWriter csvWriter,
        IAppSettingsService appSettingsService,
        ILogger<CompositeRunReportWriter> logger
    )
    {
        _markdownWriter = markdownWriter;
        _htmlWriter = htmlWriter;
        _csvWriter = csvWriter;
        _appSettingsService = appSettingsService;
        _logger = logger;
    }

    public async Task WriteAsync(RunReport report, CancellationToken cancellationToken = default)
    {
        RunReportSettings reportSettings = _appSettingsService.Current.RunReports;

        if (!reportSettings.GenerateRunReports)
        {
            _logger.LogInformation(
                "Run report generation is disabled in settings — skipping for session {SessionId}.",
                report.SessionId
            );
            return;
        }

        List<Exception>? failures = null;
        bool anyWriterEnabled = false;

        if (reportSettings.WriteMarkdown)
        {
            anyWriterEnabled = true;
            await TryWriteAsync(_markdownWriter, "markdown", report, cancellationToken).ConfigureAwait(false);
        }

        if (reportSettings.WriteCsv)
        {
            anyWriterEnabled = true;
            await TryWriteAsync(_csvWriter, "csv", report, cancellationToken).ConfigureAwait(false);
        }

        if (reportSettings.WriteHtml)
        {
            anyWriterEnabled = true;
            await TryWriteAsync(_htmlWriter, "html", report, cancellationToken).ConfigureAwait(false);
        }

        if (!anyWriterEnabled)
        {
            _logger.LogWarning("Run report output skipped because all report formats are disabled.");
            return;
        }

        if (failures is { Count: 1 })
            ExceptionDispatchInfo.Capture(failures[0]).Throw();

        if (failures is { Count: > 1 })
            throw new AggregateException("One or more run report writers failed.", failures);

        async Task TryWriteAsync(
            IRunReportWriter writer,
            string format,
            RunReport currentReport,
            CancellationToken currentCancellationToken
        )
        {
            try
            {
                await writer.WriteAsync(currentReport, currentCancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                failures ??= [];
                failures.Add(ex);
                _logger.LogError(
                    ex,
                    "Failed to write {Format} run report artifact for session {SessionId}",
                    format,
                    currentReport.SessionId
                );
            }
        }
    }
}
