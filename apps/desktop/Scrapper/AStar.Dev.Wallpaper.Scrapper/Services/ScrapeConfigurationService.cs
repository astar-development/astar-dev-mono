using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.FilesDb.Data;
using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

public sealed class ScrapeConfigurationService(IDbContextFactory<FilesContext> contextFactory)
{
    public async Task<ScrapeConfigurationEntity> ExportScrapeConfigurationAsync(CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);

        return await context.ScrapeConfiguration
            .Include(e => e.ConnectionStrings)
            .Include(e => e.UserConfiguration)
            .Include(e => e.SearchConfiguration).ThenInclude(s => s.SearchCategories)
            .Include(e => e.ScrapeDirectories)
            .OrderByDescending(e => e.Id)
            .FirstAsync(token)
            .ConfigureAwait(false);
    }

    public async Task<Unit> ImportScrapeConfigurationAsync(ScrapeConfigurationEntity incoming, CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);

        var existing = await context.ScrapeConfiguration
            .Include(e => e.ConnectionStrings)
            .Include(e => e.UserConfiguration)
            .Include(e => e.SearchConfiguration).ThenInclude(s => s.SearchCategories)
            .Include(e => e.ScrapeDirectories)
            .OrderByDescending(e => e.Id)
            .FirstAsync(token)
            .ConfigureAwait(false);

        existing.ConnectionStrings.Sqlite = incoming.ConnectionStrings.Sqlite;

        existing.UserConfiguration.LoginEmailAddress = incoming.UserConfiguration.LoginEmailAddress;
        existing.UserConfiguration.Username = incoming.UserConfiguration.Username;

        if (incoming.UserConfiguration.Password != ApplicationMetadata.Redacted)
            existing.UserConfiguration.Password = incoming.UserConfiguration.Password;

        if (incoming.UserConfiguration.SessionCookie != ApplicationMetadata.Redacted)
            existing.UserConfiguration.SessionCookie = incoming.UserConfiguration.SessionCookie;

        existing.SearchConfiguration.BaseUrl = incoming.SearchConfiguration.BaseUrl;
        existing.SearchConfiguration.SearchString = incoming.SearchConfiguration.SearchString;
        existing.SearchConfiguration.TopWallpapers = incoming.SearchConfiguration.TopWallpapers;
        existing.SearchConfiguration.SearchStringPrefix = incoming.SearchConfiguration.SearchStringPrefix;
        existing.SearchConfiguration.SearchStringSuffix = incoming.SearchConfiguration.SearchStringSuffix;
        existing.SearchConfiguration.Subscriptions = incoming.SearchConfiguration.Subscriptions;
        existing.SearchConfiguration.ImagePauseInSeconds = incoming.SearchConfiguration.ImagePauseInSeconds;
        existing.SearchConfiguration.StartingPageNumber = incoming.SearchConfiguration.StartingPageNumber;
        existing.SearchConfiguration.TotalPages = incoming.SearchConfiguration.TotalPages;
        existing.SearchConfiguration.SubscriptionsStartingPageNumber = incoming.SearchConfiguration.SubscriptionsStartingPageNumber;
        existing.SearchConfiguration.SubscriptionsTotalPages = incoming.SearchConfiguration.SubscriptionsTotalPages;
        existing.SearchConfiguration.TopWallpapersTotalPages = incoming.SearchConfiguration.TopWallpapersTotalPages;
        existing.SearchConfiguration.TopWallpapersStartingPageNumber = incoming.SearchConfiguration.TopWallpapersStartingPageNumber;
        existing.SearchConfiguration.LoginUrl = incoming.SearchConfiguration.LoginUrl;
        existing.SearchConfiguration.UseHeadless = incoming.SearchConfiguration.UseHeadless;
        existing.SearchConfiguration.SlowMotionDelay = incoming.SearchConfiguration.SlowMotionDelay;

        if (incoming.SearchConfiguration.ApiKey != ApplicationMetadata.Redacted)
            existing.SearchConfiguration.ApiKey = incoming.SearchConfiguration.ApiKey;

        UpsertSearchCategories(existing.SearchConfiguration, incoming.SearchConfiguration.SearchCategories);

        existing.ScrapeDirectories.RootDirectory = incoming.ScrapeDirectories.RootDirectory;
        existing.ScrapeDirectories.BaseSaveDirectory = incoming.ScrapeDirectories.BaseSaveDirectory;
        existing.ScrapeDirectories.BaseDirectory = incoming.ScrapeDirectories.BaseDirectory;
        existing.ScrapeDirectories.BaseDirectoryFamous = incoming.ScrapeDirectories.BaseDirectoryFamous;
        existing.ScrapeDirectories.SubDirectoryName = incoming.ScrapeDirectories.SubDirectoryName;

        await context.SaveChangesAsync(token).ConfigureAwait(false);

        return Unit.Value;
    }

    private static void UpsertSearchCategories(SearchConfiguration existing, ICollection<SearchCategories> incoming)
    {
        foreach (var category in incoming)
        {
            var existingCategory = existing.SearchCategories.FirstOrDefault(c => c.Id == category.Id);

            if (existingCategory is null)
            {
                existing.SearchCategories.Add(new SearchCategories
                {
                    Id = category.Id,
                    Name = category.Name,
                    LastKnownImageCount = category.LastKnownImageCount,
                    LastPageVisited = category.LastPageVisited,
                    TotalPages = category.TotalPages,
                    IncludeInSearch = category.IncludeInSearch
                });
            }
            else
            {
                existingCategory.Name = category.Name;
                existingCategory.LastKnownImageCount = category.LastKnownImageCount;
                existingCategory.LastPageVisited = category.LastPageVisited;
                existingCategory.TotalPages = category.TotalPages;
                existingCategory.IncludeInSearch = category.IncludeInSearch;
            }
        }
    }
}
