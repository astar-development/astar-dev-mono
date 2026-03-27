using AStar.Dev.OneDriveSync.Features.Accounts;
using AStar.Dev.OneDriveSync.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDriveSync.Infrastructure.Persistence;

/// <summary>
///     Single EF Core database context for OneDrive Sync (AC DB-01 through DB-03).
///
///     All entity configurations are applied via
///     <see cref="ModelBuilder.ApplyConfigurationsFromAssembly" /> — no inline
///     <c>modelBuilder.Entity&lt;T&gt;()</c> calls (AC DB-02).
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    /// <summary>Accounts — the sole PII-bearing table.</summary>
    public DbSet<Account> Accounts => Set<Account>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // AC DB-01: all DateTimeOffset properties stored as Unix milliseconds via a single
        // globally-registered converter — never duplicated per entity (DB-01).
        configurationBuilder.Properties<DateTimeOffset>()
            .HaveConversion<DateTimeOffsetToUnixMillisecondsConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // AC DB-02: configurations applied from assembly; no inline entity setup.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
