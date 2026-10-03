using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;

namespace AStarDev.WallpaperScraper.ScrapeConfiguration;

/// <summary>A range of pages to scrape.</summary>
/// <param name="Start">The page number to start from.</param>
/// <param name="Total">The total number of pages.</param>
public readonly record struct PageRange(int Start, int Total)
{
    /// <summary>Adds an error for each negative number, and one when the start exceeds the total.</summary>
    /// <param name="startProperty">The property name reported for a start error.</param>
    /// <param name="totalProperty">The property name reported for a total error.</param>
    /// <param name="errors">The collection the errors are added to.</param>
    internal void Validate(string startProperty, string totalProperty, List<ValidationError> errors)
    {
        var isValid = RootSettingsInput.RequireNotNegative(startProperty, Start, errors) & RootSettingsInput.RequireNotNegative(totalProperty, Total, errors);
        if (isValid && Start > Total) errors.Add(ValidationErrorFactory.Create(startProperty, "Must not exceed the total pages."));
    }
}

/// <summary>The page ranges scraped for each source.</summary>
/// <param name="Search">The search results range.</param>
/// <param name="Subscriptions">The subscriptions range.</param>
/// <param name="TopWallpapers">The top wallpapers range.</param>
/// <param name="HotWallpapers">The hot wallpapers range.</param>
public sealed record PageRanges(PageRange Search, PageRange Subscriptions, PageRange TopWallpapers, PageRange HotWallpapers)
{
    /// <summary>Copies the page ranges from the specified scrape configuration.</summary>
    /// <param name="entity">The scrape configuration to read.</param>
    public static PageRanges From(ScrapeConfigurationEntity entity) => new(
        new PageRange(entity.StartingPageNumber, entity.TotalPages),
        new PageRange(entity.SubscriptionsStartingPageNumber, entity.SubscriptionsTotalPages),
        new PageRange(entity.TopWallpapersStartingPageNumber, entity.TopWallpapersTotalPages),
        new PageRange(entity.HotWallpapersStartingPageNumber, entity.HotWallpapersTotalPages));

    /// <summary>Replaces the page ranges of the specified scrape configuration.</summary>
    /// <param name="entity">The scrape configuration to update.</param>
    public void ApplyTo(ScrapeConfigurationEntity entity, DateTimeOffset now)
    {
        (entity.StartingPageNumber, entity.TotalPages) = (Search.Start, Search.Total);
        (entity.SubscriptionsStartingPageNumber, entity.SubscriptionsTotalPages) = (Subscriptions.Start, Subscriptions.Total);
        (entity.TopWallpapersStartingPageNumber, entity.TopWallpapersTotalPages) = (TopWallpapers.Start, TopWallpapers.Total);
        (entity.HotWallpapersStartingPageNumber, entity.HotWallpapersTotalPages) = (HotWallpapers.Start, HotWallpapers.Total);
    }

    /// <summary>Adds an error for every invalid range, reported against the per-field property names.</summary>
    /// <param name="errors">The collection the errors are added to.</param>
    internal void Validate(List<ValidationError> errors)
    {
        Search.Validate("StartingPageNumber", "TotalPages", errors);
        Subscriptions.Validate("SubscriptionsStartingPageNumber", "SubscriptionsTotalPages", errors);
        TopWallpapers.Validate("TopWallpapersStartingPageNumber", "TopWallpapersTotalPages", errors);
        HotWallpapers.Validate("HotWallpapersStartingPageNumber", "HotWallpapersTotalPages", errors);
    }
}
