namespace AStarDev.Utilities;

/// <summary>Provides extension methods for the <see cref="TimeSpan"/> struct.</summary>
public static class TimeSpanExtensions
{
    extension(TimeSpan elapsed)
    {
        /// <summary>Determines whether the specified <see cref="TimeSpan"/> represents a time span of less than two minutes.</summary>
        /// <returns><c>true</c> if the time span is less than two minutes; otherwise, <c>false</c>.</returns>
        public bool IsJustNow => elapsed.TotalMinutes < 2;

        /// <summary>Determines whether the specified <see cref="TimeSpan"/> represents a time span of less than one hour.</summary>
        /// <returns><c>true</c> if the time span is less than one hour; otherwise, <c>false</c>.</returns>
        public bool IsMinutesAgo => elapsed.TotalHours < 1;

        /// <summary>Determines whether the specified <see cref="TimeSpan"/> represents a time span of less than one day.</summary>
        /// <returns><c>true</c> if the time span is less than one day; otherwise, <c>false</c>.</returns>
        public bool IsHoursAgo => elapsed.TotalDays < 1;

        /// <summary>Determines whether the specified <see cref="TimeSpan"/> represents a time span of less than two days.</summary>
        /// <returns><c>true</c> if the time span is less than two days; otherwise, <c>false</c>.</returns>
        public bool IsYesterday => elapsed.TotalDays < 2;

        /// <summary>Formats the specified <see cref="TimeSpan"/> using whichever unit reads best for its magnitude: seconds when under a minute, minutes when under an hour, otherwise hours.</summary>
        /// <returns>A human-readable duration, e.g. "42 seconds", "1 minute", or "2.5 hours".</returns>
        public string ToDurationString()
        {
            if (elapsed.TotalSeconds < 60) return FormatUnit(elapsed.TotalSeconds, "second");
            if (elapsed.TotalMinutes < 60) return FormatUnit(elapsed.TotalMinutes, "minute");

            return FormatUnit(elapsed.TotalHours, "hour");
        }
    }

    private static string FormatUnit(double value, string unit)
    {
        var rounded = Math.Round(value, 2);

        return $"{rounded:0.##} {unit}{(rounded == 1 ? string.Empty : "s")}";
    }
}
