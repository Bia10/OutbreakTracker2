namespace OutbreakTracker2.Application.Services.Reports;

internal static class RunReportOutputPathUtility
{
    public static string ResolveOutputDirectory(RunReportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        string configuredDirectory = string.IsNullOrWhiteSpace(options.OutputDirectory)
            ? RunReportOptions.DefaultOutputDirectory
            : options.OutputDirectory;

        return Path.IsPathRooted(configuredDirectory)
            ? configuredDirectory
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredDirectory));
    }

    public static string GetFileName(RunReport report, string extension)
    {
        ArgumentNullException.ThrowIfNull(report);

        string normalizedExtension = extension.TrimStart('.');
        return $"run_{report.SessionId}_{report.StartedAt:yyyyMMdd_HHmmss_UTC}.{normalizedExtension}";
    }
}
