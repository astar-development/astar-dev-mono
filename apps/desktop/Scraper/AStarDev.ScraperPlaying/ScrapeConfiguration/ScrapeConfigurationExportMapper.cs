using AStarDev.ControlDb.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

public static class ScrapeConfigurationExportMapper
{
    public static ScrapeConfigurationImportDocument ToImportDocument(this ScrapeConfigurationEntity entity, ApiKeyExport apiKeys)
    {
        var search = entity.SearchConfiguration;

        return new ScrapeConfigurationImportDocument
        {
            Id = entity.Id.Value,
            UserConfiguration = new UserConfigurationImportDocument
            {
                Id = entity.UserConfiguration.Id.Value,
                EmailAddress = entity.UserConfiguration.EmailAddress,
                Username = entity.UserConfiguration.Username,
                ApiKey = apiKeys == ApiKeyExport.Include ? entity.UserConfiguration.ApiKey : string.Empty
            },
            SearchConfiguration = new SearchConfigurationImportDocument
            {
                Id = search.Id.Value,
                SearchTerm = search.SearchTerm,
                MaxResults = search.MaxResults,
                SearchCategories = [.. search.SearchCategories.Select(category => new SearchCategoryImportDocument
                {
                    Id = category.Id,
                    Name = category.Name,
                    LastKnownImageCount = category.LastKnownImageCount,
                    LastPageVisited = category.LastPageVisited,
                    TotalPages = category.TotalPages,
                    IncludeInSearch = category.IncludeInSearch,
                    IsFamous = category.IsFamous,
                    IsInternet = category.IsInternet
                })],
                PersonCategories = [.. search.PersonCategories.Select(category => category.Name)]
            },
            ScrapeDirectories = new ScrapeDirectoriesImportDocument
            {
                Id = entity.ScrapeDirectories.Id.Value,
                RootDirectory = entity.ScrapeDirectories.RootDirectory,
                RootDirectoryFamous = entity.ScrapeDirectories.RootDirectoryFamous,
                SubDirectoryName = entity.ScrapeDirectories.SubDirectoryName
            },
            BaseUrl = entity.BaseUrl,
            ApiKey = apiKeys == ApiKeyExport.Include ? entity.ApiKey : string.Empty,
            SearchString = entity.SearchString,
            TopWallpapers = entity.TopWallpapers,
            HotWallpapers = entity.HotWallpapers,
            SearchStringPrefix = entity.SearchStringPrefix,
            SearchStringSuffix = entity.SearchStringSuffix,
            Subscriptions = entity.Subscriptions,
            ImagePauseInSeconds = entity.ImagePauseInSeconds,
            StartingPageNumber = entity.StartingPageNumber,
            TotalPages = entity.TotalPages,
            SubscriptionsStartingPageNumber = entity.SubscriptionsStartingPageNumber,
            SubscriptionsTotalPages = entity.SubscriptionsTotalPages,
            TopWallpapersStartingPageNumber = entity.TopWallpapersStartingPageNumber,
            TopWallpapersTotalPages = entity.TopWallpapersTotalPages,
            HotWallpapersStartingPageNumber = entity.HotWallpapersStartingPageNumber,
            HotWallpapersTotalPages = entity.HotWallpapersTotalPages,
            LoginUrl = entity.LoginUrl,
            UseHeadless = entity.UseHeadless,
            SlowMotionDelay = entity.SlowMotionDelay
        };
    }
}
