using AStar.Dev.Infrastructure.UsageDb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.UsageDb.Configurations;

/// <summary>
///     The <see cref="ApiUsageEventConfiguration" /> class containing, you guessed it, the ApiUsageEvent configuration settings
/// </summary>
public sealed class ApiUsageEventConfiguration : IEntityTypeConfiguration<ApiUsageEvent>
{
    /// <summary>
    ///     The Configure method will configure the ApiUsageEvent table in the database
    /// </summary>
    /// <param name="builder">An instance of the <see cref="ElementTypeBuilder" /> to add the configuration to</param>
    /// <returns>void</returns>
    public void Configure(EntityTypeBuilder<ApiUsageEvent> builder)
    {
        _ = builder
            .ToTable("ApiUsageEvent", "usage")
            .HasKey(apiUsageEvent => apiUsageEvent.Id);

        _ = builder.Property(usageEvent => usageEvent.ApiName).HasMaxLength(300);
        _ = builder.Property(usageEvent => usageEvent.ApiEndpoint).HasMaxLength(300);
        _ = builder.HasIndex(usageEvent => usageEvent.ApiName);

        _ = builder.HasIndex(usageEvent => usageEvent.Timestamp, "UpdatedDate_IX")
                   .IsClustered();
    }
}