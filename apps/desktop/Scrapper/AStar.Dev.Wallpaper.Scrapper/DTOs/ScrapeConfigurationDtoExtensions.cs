using ConnectionStringsDomain = AStar.Dev.Infrastructure.FilesDb.Models.ConnectionStrings;
using ScrapeConfigurationEntityDomain = AStar.Dev.Infrastructure.FilesDb.Models.ScrapeConfigurationEntity;
using ScrapeDirectoriesDomain = AStar.Dev.Infrastructure.FilesDb.Models.ScrapeDirectories;
using SearchCategoriesDomain = AStar.Dev.Infrastructure.FilesDb.Models.SearchCategories;
using SearchConfigurationDomain = AStar.Dev.Infrastructure.FilesDb.Models.SearchConfiguration;
using UserConfigurationDomain = AStar.Dev.Infrastructure.FilesDb.Models.UserConfiguration;

namespace AStar.Dev.Wallpaper.Scrapper.DTOs;

public static class ScrapeConfigurationDtoExtensions
{
    public static ScrapeConfigurationDto ToDto(this ScrapeConfigurationEntityDomain entity) => new()
    {
        ConnectionStrings = new ConnectionStringsDto { Sqlite = entity.ConnectionStrings.Sqlite },
        UserConfiguration = new UserConfigurationDto
        {
            LoginEmailAddress = entity.UserConfiguration.LoginEmailAddress,
            Username = entity.UserConfiguration.Username,
            Password = ApplicationMetadata.Redacted,
            SessionCookie = ApplicationMetadata.Redacted
        },
        SearchConfiguration = new SearchConfigurationDto
        {
            BaseUrl = entity.SearchConfiguration.BaseUrl.ToString(),
            ApiKey = ApplicationMetadata.Redacted,
            SearchCategories = [.. entity.SearchConfiguration.SearchCategories.Select(c => new SearchCategoryDto
            {
                Id                  = c.Id,
                Name                = c.Name,
                LastKnownImageCount = c.LastKnownImageCount,
                LastPageVisited     = c.LastPageVisited,
                TotalPages          = c.TotalPages,
                IncludeInSearch     = c.IncludeInSearch
            })],
            SearchString = entity.SearchConfiguration.SearchString,
            TopWallpapers = entity.SearchConfiguration.TopWallpapers,
            SearchStringPrefix = entity.SearchConfiguration.SearchStringPrefix,
            SearchStringSuffix = entity.SearchConfiguration.SearchStringSuffix,
            Subscriptions = entity.SearchConfiguration.Subscriptions,
            ImagePauseInSeconds = entity.SearchConfiguration.ImagePauseInSeconds,
            StartingPageNumber = entity.SearchConfiguration.StartingPageNumber,
            TotalPages = entity.SearchConfiguration.TotalPages,
            SubscriptionsStartingPageNumber = entity.SearchConfiguration.SubscriptionsStartingPageNumber,
            SubscriptionsTotalPages = entity.SearchConfiguration.SubscriptionsTotalPages,
            TopWallpapersTotalPages = entity.SearchConfiguration.TopWallpapersTotalPages,
            TopWallpapersStartingPageNumber = entity.SearchConfiguration.TopWallpapersStartingPageNumber,
            LoginUrl = entity.SearchConfiguration.LoginUrl.ToString(),
            UseHeadless = entity.SearchConfiguration.UseHeadless,
            SlowMotionDelay = entity.SearchConfiguration.SlowMotionDelay
        },
        ScrapeDirectories = new ScrapeDirectoriesDto
        {
            RootDirectory = entity.ScrapeDirectories.RootDirectory,
            BaseSaveDirectory = entity.ScrapeDirectories.BaseSaveDirectory,
            BaseDirectory = entity.ScrapeDirectories.BaseDirectory,
            BaseDirectoryFamous = entity.ScrapeDirectories.BaseDirectoryFamous,
            SubDirectoryName = entity.ScrapeDirectories.SubDirectoryName
        }
    };

    public static ScrapeConfigurationEntityDomain ToDomain(this ScrapeConfigurationDto dto)
    {
        var entity = new ScrapeConfigurationEntityDomain
        {
            ConnectionStrings = new ConnectionStringsDomain { Sqlite = dto.ConnectionStrings.Sqlite },
            UserConfiguration = new UserConfigurationDomain
            {
                LoginEmailAddress = dto.UserConfiguration.LoginEmailAddress,
                Username = dto.UserConfiguration.Username,
                Password = dto.UserConfiguration.Password,
                SessionCookie = dto.UserConfiguration.SessionCookie
            },
            SearchConfiguration = new SearchConfigurationDomain
            {
                BaseUrl = ParseUri(dto.SearchConfiguration.BaseUrl, "https://example.com"),
                ApiKey = dto.SearchConfiguration.ApiKey,
                SearchString = dto.SearchConfiguration.SearchString,
                TopWallpapers = dto.SearchConfiguration.TopWallpapers,
                SearchStringPrefix = dto.SearchConfiguration.SearchStringPrefix,
                SearchStringSuffix = dto.SearchConfiguration.SearchStringSuffix,
                Subscriptions = dto.SearchConfiguration.Subscriptions,
                ImagePauseInSeconds = dto.SearchConfiguration.ImagePauseInSeconds,
                StartingPageNumber = dto.SearchConfiguration.StartingPageNumber,
                TotalPages = dto.SearchConfiguration.TotalPages,
                SubscriptionsStartingPageNumber = dto.SearchConfiguration.SubscriptionsStartingPageNumber,
                SubscriptionsTotalPages = dto.SearchConfiguration.SubscriptionsTotalPages,
                TopWallpapersTotalPages = dto.SearchConfiguration.TopWallpapersTotalPages,
                TopWallpapersStartingPageNumber = dto.SearchConfiguration.TopWallpapersStartingPageNumber,
                LoginUrl = ParseUri(dto.SearchConfiguration.LoginUrl, "https://example.com/login"),
                UseHeadless = dto.SearchConfiguration.UseHeadless,
                SlowMotionDelay = dto.SearchConfiguration.SlowMotionDelay
            },
            ScrapeDirectories = new ScrapeDirectoriesDomain
            {
                RootDirectory = dto.ScrapeDirectories.RootDirectory,
                BaseSaveDirectory = dto.ScrapeDirectories.BaseSaveDirectory,
                BaseDirectory = dto.ScrapeDirectories.BaseDirectory,
                BaseDirectoryFamous = dto.ScrapeDirectories.BaseDirectoryFamous,
                SubDirectoryName = dto.ScrapeDirectories.SubDirectoryName
            }
        };

        foreach (var category in dto.SearchConfiguration.SearchCategories)
        {
            entity.SearchConfiguration.SearchCategories.Add(new SearchCategoriesDomain
            {
                Id = category.Id,
                Name = category.Name,
                LastKnownImageCount = category.LastKnownImageCount,
                LastPageVisited = category.LastPageVisited,
                TotalPages = category.TotalPages,
                IncludeInSearch = category.IncludeInSearch
            });
        }

        return entity;
    }

    private static Uri ParseUri(string? value, string fallback) => Uri.TryCreate(string.IsNullOrWhiteSpace(value) ? fallback : value, UriKind.Absolute, out var uri) ? uri : new Uri(fallback);
}
