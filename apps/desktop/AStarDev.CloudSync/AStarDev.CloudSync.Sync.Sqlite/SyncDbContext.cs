using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>The sync tables of the CloudSync SQLite database. It has its own migration history, apart from the accounts one.</summary>
/// <param name="options">The options that point the context at the database.</param>
[ExcludeFromCodeCoverage]
internal sealed class SyncDbContext(DbContextOptions<SyncDbContext> options) : DbContext(options)
{
    /// <summary>The name of this context's migration history table.</summary>
    internal const string MigrationsHistoryTable = "__SyncMigrationsHistory";

    /// <summary>The accounts' sync roots.</summary>
    public DbSet<SyncRootEntity> SyncRoots => Set<SyncRootEntity>();

    /// <summary>The accounts' selection rules.</summary>
    public DbSet<SelectionRuleEntity> SelectionRules => Set<SelectionRuleEntity>();

    public DbSet<RemoteItemEntity> RemoteItems => Set<RemoteItemEntity>();

    public DbSet<RemoteCursorEntity> RemoteCursors => Set<RemoteCursorEntity>();

    /// <summary>The accounts' sync baselines.</summary>
    public DbSet<SyncBaselineEntity> SyncBaseline => Set<SyncBaselineEntity>();

    public DbSet<SyncAttentionEntity> SyncAttention => Set<SyncAttentionEntity>();

    public DbSet<SyncAttentionPathEntity> SyncAttentionPaths => Set<SyncAttentionPathEntity>();

    /// <summary>The accounts whose scheduled syncing the user paused.</summary>
    public DbSet<AccountPauseEntity> AccountPauses => Set<AccountPauseEntity>();

    /// <summary>The app-wide automatic sync schedule: at most one row.</summary>
    public DbSet<SyncScheduleEntity> SyncSchedule => Set<SyncScheduleEntity>();

    /// <summary>The app-wide sync trash retention: at most one row.</summary>
    public DbSet<TrashRetentionEntity> TrashRetention => Set<TrashRetentionEntity>();

    /// <summary>The activity log, newest 1,000 entries per account.</summary>
    public DbSet<ActivityLogEntity> ActivityLog => Set<ActivityLogEntity>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new SyncRootEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SelectionRuleEntityConfiguration());
        modelBuilder.ApplyConfiguration(new RemoteItemEntityConfiguration());
        modelBuilder.ApplyConfiguration(new RemoteCursorEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SyncBaselineEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SyncAttentionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SyncAttentionPathEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AccountPauseEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SyncScheduleEntityConfiguration());
        modelBuilder.ApplyConfiguration(new TrashRetentionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ActivityLogEntityConfiguration());
    }
}
