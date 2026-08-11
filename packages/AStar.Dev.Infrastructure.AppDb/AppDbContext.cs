using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Infrastructure.AppDb;

/// <summary>The shared EF Core database context for the OneDrive Sync / Wallpaper Scraper desktop apps.</summary>
/// <param name="options">The EF Core options used to configure this context.</param>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    /// <summary>The configured OneDrive accounts.</summary>
    public DbSet<AccountEntity> Accounts => Set<AccountEntity>();

    /// <summary>The sync conflicts detected across all accounts.</summary>
    public DbSet<SyncConflictEntity> SyncConflicts => Set<SyncConflictEntity>();

    /// <summary>The sync jobs queued or completed across all accounts.</summary>
    public DbSet<SyncJobEntity> SyncJobs => Set<SyncJobEntity>();

    /// <summary>The last-known drive state for each account.</summary>
    public DbSet<DriveStateEntity> DriveStates => Set<DriveStateEntity>();

    /// <summary>The user-defined sync rules applied during synchronization.</summary>
    public DbSet<SyncRuleEntity> SyncRules => Set<SyncRuleEntity>();

    /// <summary>The items that have been synced between local and remote storage.</summary>
    public DbSet<SyncedItemEntity> SyncedItems => Set<SyncedItemEntity>();

    /// <summary>The file classifications assigned to scraped files.</summary>
    public DbSet<FileClassificationEntity> FileClassifications => Set<FileClassificationEntity>();

    /// <summary>The categories available for file classification.</summary>
    public DbSet<FileClassificationCategoryEntity> FileClassificationCategories => Set<FileClassificationCategoryEntity>();

    /// <summary>The scraped file details.</summary>
    public DbSet<FileDetailEntity> Files => Set<FileDetailEntity>();

    /// <summary>The file access details recorded for scraped files.</summary>
    public DbSet<FileAccessDetailEntity> FileAccessDetails => Set<FileAccessDetailEntity>();

    /// <summary>The tags to exclude when scraping.</summary>
    public DbSet<TagToIgnoreEntity> TagsToIgnore => Set<TagToIgnoreEntity>();

    /// <summary>The models to exclude when scraping.</summary>
    public DbSet<ModelToIgnoreEntity> ModelsToIgnore => Set<ModelToIgnoreEntity>();

    /// <summary>The scrape configuration entries.</summary>
    public DbSet<ScrapeConfigurationEntity> ScrapeConfiguration => Set<ScrapeConfigurationEntity>();

    /// <summary>The saved search configurations.</summary>
    public DbSet<SearchConfigurationEntity> SearchConfigurations => Set<SearchConfigurationEntity>();

    /// <summary>The categories available for search filtering.</summary>
    public DbSet<SearchCategoryEntity> SearchCategories => Set<SearchCategoryEntity>();

    /// <summary>The directories configured for scraping.</summary>
    public DbSet<ScrapeDirectoriesEntity> ScrapeDirectories => Set<ScrapeDirectoriesEntity>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.UseSqliteFriendlyConversions();
    }

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        => configurationBuilder.Properties<string>().UseCollation("NOCASE");

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.EnableSensitiveDataLogging();

        optionsBuilder
            .UseAsyncSeeding(async (context, _, cancellationToken) =>
            {
                if (!await context.Set<ScrapeConfigurationEntity>().AnyAsync(cancellationToken).ConfigureAwait(false))
                {
                    var classifications = new[]
                    {
                        new ScrapeConfigurationEntity
                        {
                            Id = 1, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                        }
                    };

                    await context.Set<ScrapeConfigurationEntity>().AddRangeAsync(classifications, cancellationToken).ConfigureAwait(false);
                }
                if (!await context.Set<FileClassificationCategoryEntity>().AnyAsync(cancellationToken).ConfigureAwait(false))
                {
                    var classifications = new[]
                    {
                        new FileClassificationCategoryEntity
                        {
                            Id = 1, Name = "Unclassified", Level = 1, IsFamous = false, IsInternet = false, IncludeInSearch = true
                        },
                        new FileClassificationCategoryEntity
                        {
                            Id = 2, Name = "Colour", Level = 1, IsFamous = false, IsInternet = false, IncludeInSearch = true
                        }
                    };

                    await context.Set<FileClassificationCategoryEntity>().AddRangeAsync(classifications, cancellationToken).ConfigureAwait(false);
                }

                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            })
            .UseSeeding((context, _) =>
            {
                var classification = context.Set<FileClassificationCategoryEntity>().FirstOrDefault(b => b.Name == "Colour");
                if (classification == null)
                {
                    context.Set<FileClassificationCategoryEntity>().Add(new FileClassificationCategoryEntity { Name = "Colour" });
                    context.SaveChanges();
                }
            });
    }
}
