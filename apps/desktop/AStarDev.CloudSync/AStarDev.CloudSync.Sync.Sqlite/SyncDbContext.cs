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

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyConfiguration(new SyncRootEntityConfiguration());
}
