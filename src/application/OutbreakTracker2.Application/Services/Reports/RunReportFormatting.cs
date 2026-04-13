using System.Globalization;
using System.Text;
using OutbreakTracker2.Outbreak.Utility;

namespace OutbreakTracker2.Application.Services.Reports;

internal static class RunReportFormatting
{
    public static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
            return string.Create(
                CultureInfo.InvariantCulture,
                $"{(int)duration.TotalHours}h {duration.Minutes}m {duration.Seconds}s"
            );

        return string.Create(CultureInfo.InvariantCulture, $"{duration.Minutes}m {duration.Seconds}s");
    }

    public static string FormatElapsed(TimeSpan elapsed)
    {
        if (elapsed.TotalHours >= 1)
            return string.Create(
                CultureInfo.InvariantCulture,
                $"+{(int)elapsed.TotalHours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}"
            );

        return string.Create(CultureInfo.InvariantCulture, $"+{elapsed.Minutes:D2}:{elapsed.Seconds:D2}");
    }

    public static string FormatScenarioTime(int scenarioFrame) => TimeUtility.GetTimeFromFrames(scenarioFrame);

    public static string FormatUtc(DateTimeOffset value) =>
        value.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);

    public static string HumanizeIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        ReadOnlySpan<char> span = value.AsSpan();
        StringBuilder builder = new(span.Length + 8);

        for (int index = 0; index < span.Length; index++)
        {
            char current = span[index];
            if (
                index > 0
                && char.IsUpper(current)
                && (char.IsLower(span[index - 1]) || (index + 1 < span.Length && char.IsLower(span[index + 1])))
            )
            {
                builder.Append(' ');
            }

            builder.Append(current);
        }

        return builder.ToString();
    }
}
