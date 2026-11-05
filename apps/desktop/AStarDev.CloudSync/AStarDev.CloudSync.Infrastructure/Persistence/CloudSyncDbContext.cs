using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.CloudSync.Infrastructure.Persistence;

/// <summary>The CloudSync SQLite database.</summary>
/// <param name="options">The options that point the context at the database.</param>
[ExcludeFromCodeCoverage]
public sealed class CloudSyncDbContext(DbContextOptions<CloudSyncDbContext> options) : DbContext(options)
{
    /// <summary>The saved accounts.</summary>
    public DbSet<AccountEntity> Accounts => Set<AccountEntity>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyConfiguration(new AccountEntityConfiguration());
}
