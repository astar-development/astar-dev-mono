namespace AStar.Dev.Wallpaper.Scrapper.DTOs;

public sealed record SearchConfigurationDto
{
    public string BaseUrl { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public List<SearchCategoryDto> SearchCategories { get; init; } = [];
    public string SearchString { get; init; } = string.Empty;
    public string TopWallpapers { get; init; } = string.Empty;
    public string SearchStringPrefix { get; init; } = string.Empty;
    public string SearchStringSuffix { get; init; } = string.Empty;
    public string Subscriptions { get; init; } = string.Empty;
    public int ImagePauseInSeconds { get; init; }
    public int StartingPageNumber { get; init; }
    public int TotalPages { get; init; }
    public int SubscriptionsStartingPageNumber { get; init; }
    public int SubscriptionsTotalPages { get; init; }
    public int TopWallpapersTotalPages { get; init; }
    public int TopWallpapersStartingPageNumber { get; init; }
    public string LoginUrl { get; init; } = string.Empty;
    public bool UseHeadless { get; init; }
    public float? SlowMotionDelay { get; init; }
}
