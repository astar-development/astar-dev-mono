using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AccountEntity> Accounts => Set<AccountEntity>();
    public DbSet<SyncConflictEntity> SyncConflicts => Set<SyncConflictEntity>();
    public DbSet<SyncJobEntity> SyncJobs => Set<SyncJobEntity>();
    public DbSet<DriveStateEntity> DriveStates => Set<DriveStateEntity>();
    public DbSet<SyncRuleEntity> SyncRules => Set<SyncRuleEntity>();
    public DbSet<SyncedItemEntity> SyncedItems => Set<SyncedItemEntity>();
    public DbSet<SyncedItemFileClassificationEntity> SyncedItemFileClassifications => Set<SyncedItemFileClassificationEntity>();
    public DbSet<FileClassificationCategoryEntity> FileClassificationCategories => Set<FileClassificationCategoryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseSqliteFriendlyConversions();

        _ = modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
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
