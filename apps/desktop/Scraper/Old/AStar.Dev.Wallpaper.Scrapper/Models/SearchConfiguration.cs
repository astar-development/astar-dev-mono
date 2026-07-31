namespace AStar.Dev.Wallpaper.Scrapper.Models;

public record SearchConfiguration(string BaseUrl, string ApiKey, string LoginUrl, Category[] SearchCategories, string SearchString, string TopWallpapers, string SearchStringPrefix, string SearchStringSuffix, string Subscriptions, int ImagePauseInSeconds, int StartingPageNumber, int TotalPages, bool UseHeadless, float? SlowMotionDelay, int SubscriptionsStartingPageNumber, int SubscriptionsTotalPages, int TopWallpapersTotalPages, int TopWallpapersStartingPageNumber);
