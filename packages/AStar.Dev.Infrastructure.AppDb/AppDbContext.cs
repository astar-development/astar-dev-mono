using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Infrastructure.AppDb;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AccountEntity> Accounts => Set<AccountEntity>();
    public DbSet<SyncConflictEntity> SyncConflicts => Set<SyncConflictEntity>();
    public DbSet<SyncJobEntity> SyncJobs => Set<SyncJobEntity>();
    public DbSet<DriveStateEntity> DriveStates => Set<DriveStateEntity>();
    public DbSet<SyncRuleEntity> SyncRules => Set<SyncRuleEntity>();
    public DbSet<SyncedItemEntity> SyncedItems => Set<SyncedItemEntity>();
    public DbSet<FileClassificationEntity> FileClassifications => Set<FileClassificationEntity>();
    public DbSet<FileClassificationCategoryEntity> FileClassificationCategories => Set<FileClassificationCategoryEntity>();
    public DbSet<FileDetailEntity> Files => Set<FileDetailEntity>();
    public DbSet<FileAccessDetailEntity> FileAccessDetails => Set<FileAccessDetailEntity>();
    public DbSet<FileClassificationKeywordEntity> FileClassificationKeywords => Set<FileClassificationKeywordEntity>();
    public DbSet<TagToIgnoreEntity> TagsToIgnore => Set<TagToIgnoreEntity>();
    public DbSet<ModelToIgnoreEntity> ModelsToIgnore => Set<ModelToIgnoreEntity>();
    public DbSet<ScrapeConfigurationEntity> ScrapeConfiguration => Set<ScrapeConfigurationEntity>();
    public DbSet<SearchConfigurationEntity> SearchConfigurations => Set<SearchConfigurationEntity>();
    public DbSet<ScrapedTagEntity> ScrapedTags => Set<ScrapedTagEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.UseSqliteFriendlyConversions();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        optionsBuilder
            .UseAsyncSeeding(async (context, _, cancellationToken) =>
            {
                if (!await context.Set<FileClassificationCategoryEntity>().AnyAsync(cancellationToken))
                {
                    var classifications = new[]
                    {
                        new FileClassificationCategoryEntity
                        {
                            Id = 1, Name = "Colour", Level = 1, IsFamous = false, IsInternet = false
                        }
                    };

                    await context.Set<FileClassificationCategoryEntity>().AddRangeAsync(classifications, cancellationToken);
                    await context.SaveChangesAsync(cancellationToken);
                }
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
