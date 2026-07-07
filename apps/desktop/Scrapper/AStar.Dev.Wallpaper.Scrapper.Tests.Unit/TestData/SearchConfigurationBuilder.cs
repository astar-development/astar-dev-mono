using AStar.Dev.Wallpaper.Scrapper.Models;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.TestData;

internal sealed class SearchConfigurationBuilder
{
    public string BaseUrl { get; init; } = "https://example.test";
    public string ApiKey { get; init; } = string.Empty;
    public string LoginUrl { get; init; } = "https://example.test/login";
    public Category[] SearchCategories { get; init; } = [];
    public string SearchString { get; init; } = string.Empty;
    public string TopWallpapers { get; init; } = "https://example.test/top-wallpapers/page/";
    public string SearchStringPrefix { get; init; } = "https://example.test/search/";
    public string SearchStringSuffix { get; init; } = "/?page=";
    public string Subscriptions { get; init; } = "https://example.test/subscriptions/";
    public int ImagePauseInSeconds { get; init; }
    public int StartingPageNumber { get; init; } = 1;
    public int TotalPages { get; init; } = 1;
    public bool UseHeadless { get; init; } = true;
    public float? SlowMotionDelay { get; init; }
    public int SubscriptionsStartingPageNumber { get; init; } = 1;
    public int SubscriptionsTotalPages { get; init; } = 1;
    public int TopWallpapersTotalPages { get; init; } = 1;
    public int TopWallpapersStartingPageNumber { get; init; } = 1;

    public SearchConfiguration Build()
        => new(BaseUrl, ApiKey, LoginUrl, SearchCategories, SearchString, TopWallpapers, SearchStringPrefix, SearchStringSuffix, Subscriptions, ImagePauseInSeconds, StartingPageNumber, TotalPages, UseHeadless, SlowMotionDelay, SubscriptionsStartingPageNumber, SubscriptionsTotalPages, TopWallpapersTotalPages, TopWallpapersStartingPageNumber);
}
