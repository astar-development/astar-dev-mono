using AStarDev.FunctionalParadigm;
using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels.Common;

/// <summary>Formats a past time relative to now, e.g. "5 min ago" or "Yesterday".</summary>
public static class RelativeTimeFormatter
{
    /// <summary>Formats <paramref name="occurredAt"/> relative to <paramref name="timeProvider"/>'s current time.</summary>
    /// <param name="occurredAt">The past time. A future time reads as "Just now".</param>
    /// <param name="localization">Supplies the text and culture.</param>
    /// <param name="timeProvider">Supplies the current time.</param>
    /// <returns>The localised relative time.</returns>
    public static string Format(DateTimeOffset occurredAt, ILocalizationService localization, TimeProvider timeProvider)
    {
        var elapsed = timeProvider.GetUtcNow() - occurredAt;
        if (elapsed < TimeSpan.FromMinutes(1))
            return localization.GetLocal("Time.JustNow");

        if (elapsed < TimeSpan.FromHours(1))
            return localization.GetLocal("Time.MinutesAgo", (int)elapsed.TotalMinutes);

        if (elapsed < TimeSpan.FromDays(1))
            return localization.GetLocal("Time.HoursAgo", (int)elapsed.TotalHours);

        if (elapsed < TimeSpan.FromDays(2))
            return localization.GetLocal("Time.Yesterday");

        return localization.GetLocal("Time.DaysAgo", (int)elapsed.TotalDays);
    }

    /// <summary>Formats an optional past time, reading "Never" when there is none.</summary>
    /// <param name="occurredAt">The past time, if any.</param>
    /// <param name="localization">Supplies the text and culture.</param>
    /// <param name="timeProvider">Supplies the current time.</param>
    /// <returns>The localised relative time, or "Never".</returns>
    public static string Format(Option<DateTimeOffset> occurredAt, ILocalizationService localization, TimeProvider timeProvider) => occurredAt.Match(time => Format(time, localization, timeProvider), () => localization.GetLocal("Time.Never"));
}
