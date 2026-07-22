using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Support;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scraper.Services;

public sealed class ScrapeConfigurationService(IDbContextFactory<AppDbContext> contextFactory, ScrapeConfigurationManager scrapeConfigurationManager)
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

        UpdateConnectionStrings(existing.ConnectionStrings, incoming.ConnectionStrings);
        UpdateUserConfiguration(existing.UserConfiguration, incoming.UserConfiguration);
        UpdateSearchConfiguration(existing.SearchConfiguration, incoming.SearchConfiguration);
        UpsertSearchCategories(existing.SearchConfiguration, incoming.SearchConfiguration.SearchCategories);
        UpdateScrapeDirectories(existing.ScrapeDirectories, incoming.ScrapeDirectories);

        await context.SaveChangesAsync(token).ConfigureAwait(false);
        await scrapeConfigurationManager.ReloadAsync(token).ConfigureAwait(false);

        return Unit.Instance;
    }

    private static void UpdateConnectionStrings(ConnectionStringsEntity existing, ConnectionStringsEntity incoming)
        => existing.Sqlite = incoming.Sqlite;

    private static void UpdateUserConfiguration(UserConfigurationEntity existing, UserConfigurationEntity incoming)
    {
        existing.LoginEmailAddress = incoming.LoginEmailAddress;
        existing.Username = incoming.Username;

        if (incoming.Password != ApplicationMetadata.Redacted)
            existing.Password = incoming.Password;

        if (incoming.SessionCookie != ApplicationMetadata.Redacted)
            existing.SessionCookie = incoming.SessionCookie;
    }

    private static void UpdateSearchConfiguration(SearchConfigurationEntity existing, SearchConfigurationEntity incoming)
    {
        existing.BaseUrl = incoming.BaseUrl;
        existing.SearchString = incoming.SearchString;
        existing.TopWallpapers = incoming.TopWallpapers;
        existing.SearchStringPrefix = incoming.SearchStringPrefix;
        existing.SearchStringSuffix = incoming.SearchStringSuffix;
        existing.Subscriptions = incoming.Subscriptions;
        existing.ImagePauseInSeconds = incoming.ImagePauseInSeconds;
        existing.StartingPageNumber = incoming.StartingPageNumber;
        existing.TotalPages = incoming.TotalPages;
        existing.SubscriptionsStartingPageNumber = incoming.SubscriptionsStartingPageNumber;
        existing.SubscriptionsTotalPages = incoming.SubscriptionsTotalPages;
        existing.TopWallpapersTotalPages = incoming.TopWallpapersTotalPages;
        existing.TopWallpapersStartingPageNumber = incoming.TopWallpapersStartingPageNumber;
        existing.LoginUrl = incoming.LoginUrl;
        existing.UseHeadless = incoming.UseHeadless;
        existing.SlowMotionDelay = incoming.SlowMotionDelay;

        if (incoming.ApiKey != ApplicationMetadata.Redacted)
            existing.ApiKey = incoming.ApiKey;
    }

    private static void UpdateScrapeDirectories(ScrapeDirectoriesEntity existing, ScrapeDirectoriesEntity incoming)
    {
        existing.RootDirectory = incoming.RootDirectory;
        existing.BaseSaveDirectory = incoming.BaseSaveDirectory;
        existing.BaseDirectory = incoming.BaseDirectory;
        existing.BaseDirectoryFamous = incoming.BaseDirectoryFamous;
        existing.SubDirectoryName = incoming.SubDirectoryName;
    }

    private static void UpsertSearchCategories(SearchConfigurationEntity existing, ICollection<SearchCategoryEntity> incoming)
    {
        foreach (var category in incoming)
        {
            var existingCategory = existing.SearchCategories.FirstOrDefault(c => c.Id == category.Id);

            if (existingCategory is null)
            {
                existing.SearchCategories.Add(new SearchCategoryEntity
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
