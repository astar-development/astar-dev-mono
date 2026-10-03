using AStarDev.ControlDb.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <summary>The validated root-level settings of a scrape configuration.</summary>
public sealed record RootSettings(
    WallhavenUrls Urls,
    string ApiKey,
    string SearchString,
    string SearchStringPrefix,
    string SearchStringSuffix,
    int ImagePauseInSeconds,
    PageRanges Pages,
    BrowserOptions Browser) : IScrapeConfigurationSectionEdit
{
    /// <summary>Copies the root-level settings from the specified scrape configuration.</summary>
    /// <param name="entity">The scrape configuration to read.</param>
    public static RootSettings From(ScrapeConfigurationEntity entity) => new(
        WallhavenUrls.From(entity),
        entity.ApiKey,
        entity.SearchString,
        entity.SearchStringPrefix,
        entity.SearchStringSuffix,
        entity.ImagePauseInSeconds,
        PageRanges.From(entity),
        BrowserOptions.From(entity));

    /// <inheritdoc/>
    public void ApplyTo(ScrapeConfigurationEntity entity, DateTimeOffset now)
    {
        Urls.ApplyTo(entity, now);
        Pages.ApplyTo(entity, now);
        Browser.ApplyTo(entity, now);
        entity.ApiKey = ApiKey;
        entity.SearchString = SearchString;
        entity.SearchStringPrefix = SearchStringPrefix;
        entity.SearchStringSuffix = SearchStringSuffix;
        entity.ImagePauseInSeconds = ImagePauseInSeconds;
    }

    /// <summary>Converts the settings to their editable text form.</summary>
    public RootSettingsInput ToInput() => new(Urls.ToInput(), ApiKey, SearchString, SearchStringPrefix, SearchStringSuffix, ImagePauseInSeconds, Pages, Browser);
}
