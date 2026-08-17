using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.AppDb.Configuration;

/// <summary>EF Core configuration for <see cref="ConnectionStringsEntity"/>.</summary>
public sealed class ConnectionStringsEntityConfiguration : IEntityTypeConfiguration<ConnectionStringsEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ConnectionStringsEntity> builder)
    {
        _ = builder.ToTable("ConnectionStrings");
        _ = builder.HasKey(connectionStrings => connectionStrings.Id);
        _ = builder.HasIndex(connectionStrings => connectionStrings.ScrapeConfigurationEntityId).IsUnique();
        _ = builder.Property(connectionStrings => connectionStrings.ScrapeConfigurationEntityId).IsRequired();
        _ = builder.Property(connectionStrings => connectionStrings.Sqlite).HasMaxLength(256).IsRequired();
    }
}
