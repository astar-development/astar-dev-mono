using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Lets <c>dotnet ef</c> create the context when it adds migrations. The app never uses it.</summary>
[ExcludeFromCodeCoverage]
internal sealed class SyncDesignTimeDbContextFactory : IDesignTimeDbContextFactory<SyncDbContext>
{
    /// <inheritdoc />
    public SyncDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<SyncDbContext>().UseSqlite("Data Source=design-time.db", sqlite => sqlite.MigrationsHistoryTable(SyncDbContext.MigrationsHistoryTable)).Options);
}
