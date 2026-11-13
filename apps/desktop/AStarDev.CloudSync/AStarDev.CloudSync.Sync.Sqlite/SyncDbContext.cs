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

    /// <inheritdoc />
    /// <summary>The status each account's last sync run left.</summary>
    public DbSet<SyncOutcomeEntity> SyncOutcomes => Set<SyncOutcomeEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new SyncRootEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SelectionRuleEntityConfiguration());
        modelBuilder.ApplyConfiguration(new RemoteItemEntityConfiguration());
        modelBuilder.ApplyConfiguration(new RemoteCursorEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SyncBaselineEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SyncAttentionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SyncAttentionPathEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SyncOutcomeEntityConfiguration());
    }
}
