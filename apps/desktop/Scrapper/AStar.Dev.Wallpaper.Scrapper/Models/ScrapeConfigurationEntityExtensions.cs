using AStar.Dev.Infrastructure.FilesDb.Models;

namespace AStar.Dev.Wallpaper.Scrapper.Models;

public static class ScrapeConfigurationEntityExtensions
{
    public static ScrapeConfiguration ToAppModel(this ScrapeConfigurationEntity entity)
        => new(
            new ConnectionStrings(entity.ConnectionStrings.Sqlite),
            ToUserConfiguration(entity),
            ToSearchConfiguration(entity),
            ToScrapeDirectoriesAppModel(entity));

    private static UserConfiguration ToUserConfiguration(ScrapeConfigurationEntity entity)
        => new(entity.UserConfiguration.LoginEmailAddress, entity.UserConfiguration.Username, entity.UserConfiguration.Password, entity.UserConfiguration.SessionCookie);

    private static SearchConfiguration ToSearchConfiguration(ScrapeConfigurationEntity entity)
        => new(
                    entity.SearchConfiguration.BaseUrl,
                    entity.SearchConfiguration.ApiKey,
                    entity.SearchConfiguration.LoginUrl,
                    [.. entity.SearchConfiguration.SearchCategories.Select(c => new Category
                        {
                            Id                  = c.Id,
                            Name                = c.Name,
                            LastKnownImageCount = c.LastKnownImageCount,
                            LastPageVisited     = c.LastPageVisited,
                            TotalPages          = c.TotalPages,
                        })],
                    entity.SearchConfiguration.SearchString,
                    entity.SearchConfiguration.TopWallpapers,
                    entity.SearchConfiguration.SearchStringPrefix,
                    entity.SearchConfiguration.SearchStringSuffix,
                    entity.SearchConfiguration.Subscriptions,
                    entity.SearchConfiguration.ImagePauseInSeconds,
                    entity.SearchConfiguration.StartingPageNumber,
                    entity.SearchConfiguration.TotalPages,
                    entity.SearchConfiguration.UseHeadless,
                    entity.SearchConfiguration.SlowMotionDelay,
                    entity.SearchConfiguration.SubscriptionsStartingPageNumber,
                    entity.SearchConfiguration.SubscriptionsTotalPages,
                    entity.SearchConfiguration.TopWallpapersTotalPages,
                    entity.SearchConfiguration.TopWallpapersStartingPageNumber);

    private static ScrapeDirectories ToScrapeDirectoriesAppModel(ScrapeConfigurationEntity entity)
        => new(entity.ScrapeDirectories.RootDirectory, entity.ScrapeDirectories.BaseSaveDirectory, entity.ScrapeDirectories.BaseDirectory,
                entity.ScrapeDirectories.BaseDirectoryFamous, entity.ScrapeDirectories.SubDirectoryName);
}
