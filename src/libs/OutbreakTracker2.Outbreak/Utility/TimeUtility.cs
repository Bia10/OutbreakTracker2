namespace OutbreakTracker2.Outbreak.Utility;

public static class TimeUtility
{
    private const double MillisecondsPerTenthOfSecond = 100.0;
    private const double MillisecondsPerSecond = 1000.0;
    private const double MillisecondsPerMinute = 60000.0;
    private const double MillisecondsPerHour = 3600000.0;

    private const int SecondsPerMinute = 60;
    private const int MinutesPerHour = 60;
    private const int HoursPerDay = 24;

    private const double SecondsPerFrameAt10Fps = 0.1; // 1/10th of a second (10 FPS)
    private const double SecondsPerFrameAt15Fps = 0.0667; // 1/15th of a second (15 FPS)
    private const double SecondsPerFrameAt24Fps = 0.0417; // 1/24th of a second (24 FPS)
    private const double SecondsPerFrameAt30Fps = 0.0333; // 1/30th of a second (30 FPS)
    private const double SecondsPerFrameAt60Fps = 0.0167; // 1/60th of a second (60 FPS)
    private const double SecondsPerFrameAt90Fps = 0.0111; // 1/90th of a second (90 FPS)
    private const double SecondsPerFrameAt120Fps = 0.0083; // 1/120th of a second (120 FPS)

    // TODO: not rly sure why there are special units of time
    // doesn't appear to be frames as one is 30fps other 60fps
    private const int StandardTimeUnitsPerSecond = 1;
    private const int WildThingsTimeUnitsPerSecond = 30;
    private const int StoppingVirusTimeUnitsPerSecond = 60;

    // 30 * 60 = 1800
    private const int WildThingsTimeUnitsPerMinute = WildThingsTimeUnitsPerSecond * SecondsPerMinute;

    // 60 * 60 = 3600
    private const int StoppingVirusTimeUnitsPerMinute = StoppingVirusTimeUnitsPerSecond * SecondsPerMinute;

    /// <summary>
    /// Converts a frame count into a formatted time string (HH:MM:SS.D)
    /// based on a provided time per frame duration.
    /// </summary>
    /// <param name="frameCount">The current frame count.</param>
    /// <param name="secondsPerFrame">The duration of each frame in seconds.
    /// Defaults to 0.0333, which is approximately 1/30th of a second (30 FPS).</param>
    /// <returns>A string representing the time in HH:MM:SS.D format.</returns>
    public static string GetTimeFromFrames(double frameCount, double secondsPerFrame = SecondsPerFrameAt30Fps)
    {
        double totalSecondsElapsed = frameCount * secondsPerFrame;
        double totalMilliseconds = totalSecondsElapsed * MillisecondsPerSecond;

        double totalHoursElapsed = totalMilliseconds / MillisecondsPerHour;
        int hours = (int)Math.Floor(totalHoursElapsed % HoursPerDay);

        double totalMinutesElapsed = totalMilliseconds / MillisecondsPerMinute;
        int minutes = (int)Math.Floor(totalMinutesElapsed % MinutesPerHour);

        double totalSecondsFromMilliseconds = totalMilliseconds / MillisecondsPerSecond;
        int seconds = (int)Math.Floor(totalSecondsFromMilliseconds % SecondsPerMinute);

        int tenthsOfSecond = (int)Math.Floor(totalMilliseconds % MillisecondsPerSecond / MillisecondsPerTenthOfSecond);

        return $"{hours:D2}:{minutes:D2}:{seconds:D2}.{tenthsOfSecond:D1}";
    }

    public static string GetStandardTimeToString(int timeInSeconds)
    {
        int minutes = (int)Math.Floor((double)timeInSeconds / StandardTimeUnitsPerSecond / SecondsPerMinute % MinutesPerHour);
        int seconds = (int)Math.Floor(((double)timeInSeconds - SecondsPerMinute) % SecondsPerMinute);

        return $"{minutes:D2}:{seconds:D2}";
    }

    public static string GetTimeToString3(int wildThingsTimeUnits)
    {
        int minutes = (int)Math.Floor((double)wildThingsTimeUnits / WildThingsTimeUnitsPerMinute % MinutesPerHour);
        int seconds = (int)Math.Floor((double)wildThingsTimeUnits / WildThingsTimeUnitsPerSecond % SecondsPerMinute);

        return $"{minutes:D2}:{seconds:D2}";
    }

    public static string GetStoppingVirusTime(int stoppingVirusTimeUnits)
    {
        int minutes = (int)Math.Floor((double)stoppingVirusTimeUnits / StoppingVirusTimeUnitsPerMinute % MinutesPerHour);
        int seconds = (int)Math.Floor((double)stoppingVirusTimeUnits / StoppingVirusTimeUnitsPerSecond % SecondsPerMinute);

        return $"{minutes}:{seconds:D2}";
    }
}