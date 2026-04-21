using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AccountEntity> Accounts => Set<AccountEntity>();
    public DbSet<SyncFolderEntity> SyncFolders => Set<SyncFolderEntity>();
    public DbSet<SyncConflictEntity> SyncConflicts => Set<SyncConflictEntity>();
    public DbSet<SyncJobEntity> SyncJobs => Set<SyncJobEntity>();
    public DbSet<DriveStateEntity> DriveStates => Set<DriveStateEntity>();
    public DbSet<SyncRuleEntity> SyncRules => Set<SyncRuleEntity>();
    public DbSet<SyncedItemEntity> SyncedItems => Set<SyncedItemEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseSqliteFriendlyConversions();

        _ = modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
