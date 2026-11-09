using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AStarDev.CloudSync.Accounts.Sqlite;

/// <summary>Lets <c>dotnet ef</c> create the context when it adds migrations. The app never uses it.</summary>
[ExcludeFromCodeCoverage]
internal sealed class CloudSyncDesignTimeDbContextFactory : IDesignTimeDbContextFactory<CloudSyncDbContext>
{
    /// <inheritdoc />
    public CloudSyncDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<CloudSyncDbContext>().UseSqlite("Data Source=design-time.db").Options);
}
