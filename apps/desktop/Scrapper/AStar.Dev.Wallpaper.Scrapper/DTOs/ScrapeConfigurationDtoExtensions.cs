using ScrapeConfigurationEntityDomain = AStar.Dev.Infrastructure.FilesDb.Models.ScrapeConfigurationEntity;
using SearchCategoriesDomain = AStar.Dev.Infrastructure.FilesDb.Models.SearchCategories;
using ConnectionStringsDomain = AStar.Dev.Infrastructure.FilesDb.Models.ConnectionStrings;
using UserConfigurationDomain = AStar.Dev.Infrastructure.FilesDb.Models.UserConfiguration;
using SearchConfigurationDomain = AStar.Dev.Infrastructure.FilesDb.Models.SearchConfiguration;
using ScrapeDirectoriesDomain = AStar.Dev.Infrastructure.FilesDb.Models.ScrapeDirectories;

namespace AStar.Dev.Wallpaper.Scrapper.DTOs;

public static class ScrapeConfigurationDtoExtensions
{
    public static ScrapeConfigurationDto ToDto(this ScrapeConfigurationEntityDomain entity) => new()
    {
        ConnectionStrings = new ConnectionStringsDto { Sqlite = entity.ConnectionStrings.Sqlite },
        UserConfiguration = new UserConfigurationDto
        {
            LoginEmailAddress = entity.UserConfiguration.LoginEmailAddress,
            Username          = entity.UserConfiguration.Username,
            Password          = ApplicationMetadata.Redacted,
            SessionCookie     = ApplicationMetadata.Redacted
        },
        SearchConfiguration = new SearchConfigurationDto
        {
            BaseUrl                         = entity.SearchConfiguration.BaseUrl,
            ApiKey                          = ApplicationMetadata.Redacted,
            SearchCategories                = [.. entity.SearchConfiguration.SearchCategories.Select(c => new SearchCategoryDto
            {
                Id                  = c.Id,
                Name                = c.Name,
                LastKnownImageCount = c.LastKnownImageCount,
                LastPageVisited     = c.LastPageVisited,
                TotalPages          = c.TotalPages,
                IncludeInSearch     = c.IncludeInSearch
            })],
            SearchString                    = entity.SearchConfiguration.SearchString,
            TopWallpapers                   = entity.SearchConfiguration.TopWallpapers,
            SearchStringPrefix              = entity.SearchConfiguration.SearchStringPrefix,
            SearchStringSuffix              = entity.SearchConfiguration.SearchStringSuffix,
            Subscriptions                   = entity.SearchConfiguration.Subscriptions,
            ImagePauseInSeconds             = entity.SearchConfiguration.ImagePauseInSeconds,
            StartingPageNumber              = entity.SearchConfiguration.StartingPageNumber,
            TotalPages                      = entity.SearchConfiguration.TotalPages,
            SubscriptionsStartingPageNumber = entity.SearchConfiguration.SubscriptionsStartingPageNumber,
            SubscriptionsTotalPages         = entity.SearchConfiguration.SubscriptionsTotalPages,
            TopWallpapersTotalPages         = entity.SearchConfiguration.TopWallpapersTotalPages,
            TopWallpapersStartingPageNumber = entity.SearchConfiguration.TopWallpapersStartingPageNumber,
            LoginUrl                        = entity.SearchConfiguration.LoginUrl,
            UseHeadless                     = entity.SearchConfiguration.UseHeadless,
            SlowMotionDelay                 = entity.SearchConfiguration.SlowMotionDelay
        },
        ScrapeDirectories = new ScrapeDirectoriesDto
        {
            RootDirectory       = entity.ScrapeDirectories.RootDirectory,
            BaseSaveDirectory   = entity.ScrapeDirectories.BaseSaveDirectory,
            BaseDirectory       = entity.ScrapeDirectories.BaseDirectory,
            BaseDirectoryFamous = entity.ScrapeDirectories.BaseDirectoryFamous,
            SubDirectoryName    = entity.ScrapeDirectories.SubDirectoryName
        }
    };

    public static ScrapeConfigurationEntityDomain ToDomain(this ScrapeConfigurationDto dto) => new()
    {
        ConnectionStrings = new ConnectionStringsDomain { Sqlite = dto.ConnectionStrings.Sqlite },
        UserConfiguration = new UserConfigurationDomain
        {
            LoginEmailAddress = dto.UserConfiguration.LoginEmailAddress,
            Username          = dto.UserConfiguration.Username,
            Password          = dto.UserConfiguration.Password,
            SessionCookie     = dto.UserConfiguration.SessionCookie
        },
        SearchConfiguration = new SearchConfigurationDomain
        {
            BaseUrl                         = dto.SearchConfiguration.BaseUrl,
            ApiKey                          = dto.SearchConfiguration.ApiKey,
            SearchCategories                = [.. dto.SearchConfiguration.SearchCategories.Select(c => new SearchCategoriesDomain
            {
                Id                  = c.Id,
                Name                = c.Name,
                LastKnownImageCount = c.LastKnownImageCount,
                LastPageVisited     = c.LastPageVisited,
                TotalPages          = c.TotalPages,
                IncludeInSearch     = c.IncludeInSearch
            })],
            SearchString                    = dto.SearchConfiguration.SearchString,
            TopWallpapers                   = dto.SearchConfiguration.TopWallpapers,
            SearchStringPrefix              = dto.SearchConfiguration.SearchStringPrefix,
            SearchStringSuffix              = dto.SearchConfiguration.SearchStringSuffix,
            Subscriptions                   = dto.SearchConfiguration.Subscriptions,
            ImagePauseInSeconds             = dto.SearchConfiguration.ImagePauseInSeconds,
            StartingPageNumber              = dto.SearchConfiguration.StartingPageNumber,
            TotalPages                      = dto.SearchConfiguration.TotalPages,
            SubscriptionsStartingPageNumber = dto.SearchConfiguration.SubscriptionsStartingPageNumber,
            SubscriptionsTotalPages         = dto.SearchConfiguration.SubscriptionsTotalPages,
            TopWallpapersTotalPages         = dto.SearchConfiguration.TopWallpapersTotalPages,
            TopWallpapersStartingPageNumber = dto.SearchConfiguration.TopWallpapersStartingPageNumber,
            LoginUrl                        = dto.SearchConfiguration.LoginUrl,
            UseHeadless                     = dto.SearchConfiguration.UseHeadless,
            SlowMotionDelay                 = dto.SearchConfiguration.SlowMotionDelay
        },
        ScrapeDirectories = new ScrapeDirectoriesDomain
        {
            RootDirectory       = dto.ScrapeDirectories.RootDirectory,
            BaseSaveDirectory   = dto.ScrapeDirectories.BaseSaveDirectory,
            BaseDirectory       = dto.ScrapeDirectories.BaseDirectory,
            BaseDirectoryFamous = dto.ScrapeDirectories.BaseDirectoryFamous,
            SubDirectoryName    = dto.ScrapeDirectories.SubDirectoryName
        }
    };
}
