using AStar.Dev.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Infrastructure.UsageDb.Data;

/// <summary>
/// </summary>
public sealed class ApiUsageContext : DbContext
{
    private readonly ConnectionString connectionString;

    /// <summary>
    /// </summary>
    public ApiUsageContext()
        => connectionString = string.Empty;

    /// <summary>
    /// </summary>
    /// <param name="connectionString"></param>
    public ApiUsageContext(ConnectionString connectionString)
        : this()
        => this.connectionString = connectionString;

    /// <summary>
    /// </summary>
    public DbSet<ApiUsageEvent> ApiUsage { get; set; }

    /// <summary>
    ///     The overridden OnModelCreating method.
    /// </summary>
    /// <param name="modelBuilder">
    /// </param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.UseCollation("SQL_Latin1_General_CP1_CI_AS");
        _ = modelBuilder.HasDefaultSchema("usage");
        _ = modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApiUsageContext).Assembly);
    }

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => _ = optionsBuilder.UseSqlServer(connectionString.Value);
}