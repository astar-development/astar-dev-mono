using AStarDev.ControlDb.ScrapeConfiguration;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb;

/// <summary>Provides methods for seeding the database with initial data.</summary>
public static class Seeder
{
    /// <summary>Seeds the database with initial data asynchronously.</summary>
    /// <param name="context">The database context used for seeding data.</param>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous seeding operation.</returns>
    public static Task SeedAsync(DbContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        SeedImpl(context);
        return context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Seeds the database with initial data synchronously.</summary>
    /// <param name="context">The database context used for seeding data.</param>
    public static void Seed(DbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        SeedImpl(context);
        _ = context.SaveChanges();
    }

    private static void SeedImpl(DbContext context)
    {
        if (context.Set<ScrapeConfigurationEntity>().Any())
        {
            return;
        }

        var searchCategories = new List<SearchCategoryEntity>
                {
                    new(){SearchConfigurationId = new SearchConfigurationId(Guid.Empty), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, Name = "category1", IsFamous = true, IncludeInSearch = true, IsInternet = false, Id = "1"},
                    new(){SearchConfigurationId = new SearchConfigurationId(Guid.Empty), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, Name = "category2", IsFamous = false, IncludeInSearch = true, IsInternet = true, Id = "2"},
                    new(){SearchConfigurationId = new SearchConfigurationId(Guid.Empty), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, Name = "category3", IsFamous = false, IncludeInSearch = true, IsInternet = false, Id = "3"}
                };
        var searchConfiguration = new SearchConfigurationEntity(Guid.Empty, Guid.Empty, "search term", 500, searchCategories);
        foreach (var name in PersonCategoryEntity.DefaultNames)
        {
            searchConfiguration.PersonCategories.Add(new PersonCategoryEntity { SearchConfigurationId = new SearchConfigurationId(Guid.Empty), Name = name });
        }

        _ = context.Set<ScrapeConfigurationEntity>().Add(new ScrapeConfigurationEntity(Guid.Empty)
        {
            UserConfiguration = new UserConfigurationEntity(Guid.Empty, Guid.Empty, "user@example.com", "user", "n/a"),
            SearchConfiguration = searchConfiguration,
            ScrapeDirectories = new ScrapeDirectoriesEntity(Guid.Empty, Guid.Empty, DefaultRootDirectory(), Path.Combine(DefaultRootDirectory(), "Famous"), "subdirectory"),
            BaseUrl = new Uri("https://wallhaven.cc/"),
            ApiKey = "your-api-key",
            SearchString = "search string",
            TopWallpapers = "top wallpapers",
            HotWallpapers = "HotWallpapers",
            SearchStringPrefix = "/search?q=id:",
            SearchStringSuffix = "&categories=001&purity=111&sorting=date_added&order=desc&ai_art_filter=0&page=",
            SlowMotionDelay = 500,
            Subscriptions = "subscription?page=",
            UseHeadless = false
        });
    }

    /// <summary>A neutral default the user is expected to change: a WallHaven folder under their pictures folder (their profile's Pictures folder where the platform reports none).</summary>
    private static string DefaultRootDirectory()
    {
        var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        if (string.IsNullOrWhiteSpace(pictures)) pictures = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Pictures");

        return Path.Combine(pictures, "WallHaven");
    }
}
