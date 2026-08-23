using AStarDev.ControlDb.Files;
using AStarDev.ControlDb.ScrapeConfiguration;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb;

/// <summary>
/// Represents the Entity Framework database context for managing file entities.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ControlDbContext"/> class with the specified options.
/// </remarks>
/// <param name="options">The options to be used by the DbContext.</param>
public class ControlDbContext(DbContextOptions<ControlDbContext> options) : DbContext(options)
{
    /// <summary>
    ///   Gets the repository for managing file entities in the database.
    /// </summary>
    public ControlDbContext() : this(new DbContextOptions<ControlDbContext>()) { }

    /// <summary>
    /// Gets the repository for managing file entities in the database.
    /// </summary>
    public DbSet<FileEntity> Files => Set<FileEntity>();

    /// <summary>
    /// Gets the repository for managing scrape configuration entities in the database.
    /// </summary>
    public DbSet<ScrapeConfigurationEntity> ScrapeConfigurations => Set<ScrapeConfigurationEntity>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FileEntity>().Property<FileId>("Id").HasConversion(fileId => fileId.Value, value => new FileId(value)).ValueGeneratedOnAdd();
        modelBuilder.Entity<FileEntity>().HasKey("Id");

        _ = modelBuilder.ApplyConfigurationsFromAssembly(typeof(ControlDbContext).Assembly);

        modelBuilder.UseSqliteFriendlyConversions();
    }

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        => configurationBuilder.Properties<string>().UseCollation("NOCASE");

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        string tempDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!optionsBuilder.IsConfigured) _ = optionsBuilder.UseSqlite($"Data Source={tempDir}/Scraper/files.db");

        optionsBuilder
            .UseAsyncSeeding(async (context, _, cancellationToken) =>
            {
                SeedData(context);

                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                Console.WriteLine("Async Seeding ControlDbContext completed successfully...");
            })
            .UseSeeding((context, _) =>
            {
                SeedData(context);

                context.SaveChanges();

                Console.WriteLine("Sync Seeding ControlDbContext completed successfully...");
            });
    }

    private static void SeedData(DbContext context)
    {
        if (context.Set<ScrapeConfigurationEntity>().Any())
        {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            Console.WriteLine("Seeding ControlDbContext skipped as it already contains data.");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            return;
        }

        var searchCategories = new List<SearchCategoryEntity>
                {
                    new(){SearchConfigurationId = new SearchConfigurationId(Guid.Empty), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, Name = "category1", IsFamous = true, IncludeInSearch = true, IsInternet = false, Id = "1"},
                    new(){SearchConfigurationId = new SearchConfigurationId(Guid.Empty), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, Name = "category2", IsFamous = false, IncludeInSearch = true, IsInternet = true, Id = "2"},
                    new(){SearchConfigurationId = new SearchConfigurationId(Guid.Empty), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, Name = "category3", IsFamous = false, IncludeInSearch = true, IsInternet = false, Id = "3"}
                };
        context.Set<ScrapeConfigurationEntity>().Add(new ScrapeConfigurationEntity(Guid.Empty)
        {
            UserConfiguration = new UserConfigurationEntity(Guid.Empty, Guid.Empty, "jason.j.barden2@outlook.com", "jbarden", Environment.GetEnvironmentVariable("ScrapePassword") ?? "Password1!", "n/a"),
            SearchConfiguration = new SearchConfigurationEntity(Guid.Empty, Guid.Empty, "search term", 500, searchCategories)
            {
                BaseUrl = new Uri("https://wallhaven.cc/"),
                ApiKey = "your-api-key",
                SearchString = "search string",
                TopWallpapers = "top wallpapers",
                SearchStringPrefix = "/search?q=id:",
                SearchStringSuffix = "&categories=001&purity=111&sorting=date_added&order=desc&ai_art_filter=0&page=",
                SlowMotionDelay = 500,
                Subscriptions = "subscription?page=",
                UseHeadless = false
            },
            ScrapeDirectories = new ScrapeDirectoriesEntity(Guid.Empty, Guid.Empty, "/run/media/jbarden/Tbdrive/sync/jason.barden1@outlook.com/", "Pictures/WallHaven/", "Pictures/WallHaven/", "Pictures/WallHaven/Famous", "subdirectory")
        });
    }
}

