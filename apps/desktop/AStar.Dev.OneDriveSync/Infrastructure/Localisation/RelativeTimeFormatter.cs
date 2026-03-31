using System.Globalization;

namespace AStar.Dev.OneDriveSync.Infrastructure.Localisation;

/// <inheritdoc />
internal sealed class RelativeTimeFormatter(ILocalisationService localisationService) : IRelativeTimeFormatter
{
    /// <inheritdoc />
    public string Format(DateTimeOffset timestamp, DateTimeOffset now)
    {
        var diff = now - timestamp;

        if (diff < TimeSpan.FromHours(1))
            return FormatRelative(diff);

        return FormatAbsolute(timestamp, now);
    }

    private static string FormatRelative(TimeSpan diff)
    {
        var minutes = Math.Max(1, (int)diff.TotalMinutes);

        return minutes == 1 ? "1 minute ago" : $"{minutes} minutes ago";
    }

    private string FormatAbsolute(DateTimeOffset timestamp, DateTimeOffset now)
    {
        var culture = CultureInfo.GetCultureInfo(localisationService.CurrentLocale);

        if (timestamp.Date == now.Date)
            return $"Today at {timestamp.ToString("HH:mm", culture)}";

        return $"{timestamp.ToString("d MMM", culture)} at {timestamp.ToString("HH:mm", culture)}";
    }
}
