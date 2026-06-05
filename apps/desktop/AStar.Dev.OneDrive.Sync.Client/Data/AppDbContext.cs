using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
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
    public DbSet<SyncedItemClassificationEntity> SyncedItemClassifications => Set<SyncedItemClassificationEntity>();
    public DbSet<FileClassificationRuleEntity> FileClassificationRules => Set<FileClassificationRuleEntity>();
    public DbSet<FileClassificationCategoryEntity> FileClassificationCategories => Set<FileClassificationCategoryEntity>();
    public DbSet<FileClassificationKeywordEntity> FileClassificationKeywords => Set<FileClassificationKeywordEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseSqliteFriendlyConversions();

        _ = modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
