namespace AStarDev.WallpaperScraper.ScrapeConfiguration;

internal static class ScrapeSettingsImportMapper
{
    public static ScrapeConfigurationImportDocument ToImportDocument(this ScrapeSettingsImportDocument source)
    {
        var configurationId = Guid.CreateVersion7();
        var searchConfigurationId = Guid.CreateVersion7();
        var userConfiguration = source.ScrapeConfiguration.UserConfiguration;
        var searchConfiguration = source.ScrapeConfiguration.SearchConfiguration;
        var directories = source.ScrapeConfiguration.ScrapeDirectories;

        return new ScrapeConfigurationImportDocument
        {
            Id = configurationId,
            UserConfiguration = new UserConfigurationImportDocument
            {
                Id = Guid.CreateVersion7(),
                EmailAddress = userConfiguration.LoginEmailAddress,
                Username = userConfiguration.Username
            },
            SearchConfiguration = new SearchConfigurationImportDocument
            {
                Id = searchConfigurationId,
                SearchCategories = searchConfiguration.SearchCategories
            },
            ScrapeDirectories = new ScrapeDirectoriesImportDocument
            {
                Id = Guid.CreateVersion7(),
                RootDirectory = directories.BaseDirectory,
                BaseSaveDirectory = directories.BaseSaveDirectory,
                BaseDirectory = directories.BaseDirectory,
                RootDirectoryFamous = directories.BaseDirectoryFamous,
                SubDirectoryName = directories.SubDirectoryName
            },
            BaseUrl = searchConfiguration.BaseUrl,
            LoginUrl = searchConfiguration.LoginUrl,
            SearchString = searchConfiguration.SearchString,
            TopWallpapers = searchConfiguration.TopWallpapers,
            HotWallpapers = searchConfiguration.HotWallpapers,
            SearchStringPrefix = searchConfiguration.SearchStringPrefix,
            SearchStringSuffix = searchConfiguration.SearchStringSuffix,
            Subscriptions = searchConfiguration.Subscriptions,
            ImagePauseInSeconds = searchConfiguration.ImagePauseInSeconds,
            StartingPageNumber = searchConfiguration.StartingPageNumber,
            TotalPages = searchConfiguration.TotalPages,
            UseHeadless = searchConfiguration.UseHeadless,
            SlowMotionDelay = searchConfiguration.SlowMotionDelay,
            SubscriptionsStartingPageNumber = searchConfiguration.SubscriptionsStartingPageNumber,
            SubscriptionsTotalPages = searchConfiguration.SubscriptionsTotalPages,
            TopWallpapersTotalPages = searchConfiguration.TopWallpapersTotalPages,
            TopWallpapersStartingPageNumber = searchConfiguration.TopWallpapersStartingPageNumber,
            HotWallpapersTotalPages = searchConfiguration.HotWallpapersTotalPages,
            HotWallpapersStartingPageNumber = searchConfiguration.HotWallpapersStartingPageNumber
        };
    }
}
