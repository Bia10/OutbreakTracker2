using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging;

namespace OutbreakTracker2.Application.Services.Reports;

public sealed class CompositeRunReportWriter : IRunReportWriter
{
    private readonly HtmlRunReportWriter _htmlWriter;
    private readonly ILogger<CompositeRunReportWriter> _logger;
    private readonly MarkdownRunReportWriter _markdownWriter;
    private readonly RunReportOptions _options;

    public CompositeRunReportWriter(
        MarkdownRunReportWriter markdownWriter,
        HtmlRunReportWriter htmlWriter,
        RunReportOptions options,
        ILogger<CompositeRunReportWriter> logger
    )
    {
        _markdownWriter = markdownWriter;
        _htmlWriter = htmlWriter;
        _options = options;
        _logger = logger;
    }

    public async Task WriteAsync(RunReport report, CancellationToken cancellationToken = default)
    {
        List<Exception>? failures = null;
        bool anyWriterEnabled = false;

        if (_options.WriteMarkdown)
        {
            anyWriterEnabled = true;
            await TryWriteAsync(_markdownWriter, "markdown", report, cancellationToken).ConfigureAwait(false);
        }

        if (_options.WriteHtml)
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
