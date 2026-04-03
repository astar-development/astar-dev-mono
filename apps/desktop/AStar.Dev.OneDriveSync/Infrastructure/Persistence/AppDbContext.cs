using System;
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
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    /// <summary>Accounts — the sole PII-bearing table.</summary>
    public DbSet<Account> Accounts => Set<Account>();

    /// <summary>File metadata for accounts with AM-12 enabled.</summary>
    public DbSet<SyncedFileMetadata> SyncedFileMetadata => Set<SyncedFileMetadata>();

    /// <summary>Single-row application settings (theme, locale, user type).</summary>
    public DbSet<AppSettings> AppSettings => Set<AppSettings>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        => _ = configurationBuilder.Properties<DateTimeOffset>()
            .HaveConversion<DateTimeOffsetToUnixMillisecondsConverter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        _ = modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
