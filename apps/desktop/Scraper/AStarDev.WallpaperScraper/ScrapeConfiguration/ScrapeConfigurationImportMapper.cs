using AStarDev.ControlDb.ScrapeConfiguration;

namespace AStarDev.WallpaperScraper.ScrapeConfiguration;

public static class ScrapeConfigurationImportMapper
{
    public static ScrapeConfigurationEntity ToEntity(this ScrapeConfigurationImportDocument document, DateTimeOffset now)
    {
        var search = document.SearchConfiguration;
        var categories = search.SearchCategories.Select(category => new SearchCategoryEntity
        {
            SearchConfigurationId = new SearchConfigurationId(search.Id),
            Id = category.Id,
            Name = category.Name,
            LastKnownImageCount = category.LastKnownImageCount,
            LastPageVisited = category.LastPageVisited,
            TotalPages = category.TotalPages,
            IncludeInSearch = category.IncludeInSearch,
            IsFamous = category.IsFamous,
            IsInternet = category.IsInternet,
            CreatedAt = now,
            UpdatedAt = now
        }).ToList();

        var searchEntity = new SearchConfigurationEntity(
            new SearchConfigurationId(search.Id),
            new ScrapeConfigurationId(document.Id),
            search.SearchTerm,
            search.MaxResults,
            categories);

        foreach (var name in search.PersonCategories)
        {
            searchEntity.PersonCategories.Add(new PersonCategoryEntity { SearchConfigurationId = searchEntity.Id, Name = name, CreatedAt = now, UpdatedAt = now });
        }

        return new ScrapeConfigurationEntity(new ScrapeConfigurationId(document.Id))
        {
            UserConfiguration = new UserConfigurationEntity(
                new UserConfigurationId(document.UserConfiguration.Id),
                new ScrapeConfigurationId(document.Id),
                document.UserConfiguration.EmailAddress,
                document.UserConfiguration.Username,
                document.UserConfiguration.ApiKey),
            SearchConfiguration = searchEntity,
            ScrapeDirectories = new ScrapeDirectoriesEntity(
                new ScrapeDirectoriesId(document.ScrapeDirectories.Id),
                new ScrapeConfigurationId(document.Id),
                document.ScrapeDirectories.RootDirectory,
                document.ScrapeDirectories.RootDirectoryFamous,
                document.ScrapeDirectories.SubDirectoryName),
            BaseUrl = document.BaseUrl,
            ApiKey = document.ApiKey,
            SearchString = document.SearchString,
            TopWallpapers = document.TopWallpapers,
            HotWallpapers = document.HotWallpapers,
            SearchStringPrefix = document.SearchStringPrefix,
            SearchStringSuffix = document.SearchStringSuffix,
            Subscriptions = document.Subscriptions,
            ImagePauseInSeconds = document.ImagePauseInSeconds,
            StartingPageNumber = document.StartingPageNumber,
            TotalPages = document.TotalPages,
            SubscriptionsStartingPageNumber = document.SubscriptionsStartingPageNumber,
            SubscriptionsTotalPages = document.SubscriptionsTotalPages,
            TopWallpapersStartingPageNumber = document.TopWallpapersStartingPageNumber,
            TopWallpapersTotalPages = document.TopWallpapersTotalPages,
            HotWallpapersStartingPageNumber = document.HotWallpapersStartingPageNumber,
            HotWallpapersTotalPages = document.HotWallpapersTotalPages,
            LoginUrl = document.LoginUrl,
            UseHeadless = document.UseHeadless,
            SlowMotionDelay = document.SlowMotionDelay
        };
    }
}
