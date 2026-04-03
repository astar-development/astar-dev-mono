using System;
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

    private string FormatRelative(TimeSpan diff)
    {
        int minutes = Math.Max(1, (int)diff.TotalMinutes);

        return minutes == 1
            ? localisationService.GetString("RelativeTimeFormatter_OneMinuteAgo")
            : string.Format(CultureInfo.InvariantCulture, localisationService.GetString("RelativeTimeFormatter_MinutesAgo"), minutes);
    }

    private string FormatAbsolute(DateTimeOffset timestamp, DateTimeOffset now)
    {
        var culture = CultureInfo.GetCultureInfo(localisationService.CurrentLocale);

        if (timestamp.Date == now.Date)
            return localisationService.GetString("RelativeTimeFormatter_TodayAt") + timestamp.ToString("HH:mm", culture);

        return timestamp.ToString("d MMM", culture) + localisationService.GetString("RelativeTimeFormatter_DateAt") + timestamp.ToString("HH:mm", culture);
    }
}
