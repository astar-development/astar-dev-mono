using AStarDev.ControlDb.ScrapeConfiguration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.ControlDb.Configurations.ScrapeConfiguration;

/// <summary>
/// Provides the configuration for the <see cref="ScrapeConfigurationEntity"/> entity.
/// </summary>
public sealed class ConnectionStringsEntityConfiguration : IEntityTypeConfiguration<ConnectionStringsEntity>
{
    ///<inheritdoc/>
    public void Configure(EntityTypeBuilder<ConnectionStringsEntity> builder)
    {
        builder.ToTable("ConnectionStrings");

        builder.HasKey(sc => sc.Id);

        builder.Property(sc => sc.Id).ValueGeneratedOnAdd();
        builder.Property(d => d.Id).HasConversion(id => id.Value, value => new ConnectionStringId(value));
        builder.HasOne<ScrapeConfigurationEntity>()
            .WithOne(scrapeConfiguration => scrapeConfiguration.ConnectionStrings)
            .HasForeignKey<ConnectionStringsEntity>(connectionStrings => connectionStrings.ScrapeConfigurationEntityId)
            .HasPrincipalKey<ScrapeConfigurationEntity>(scrapeConfiguration => scrapeConfiguration.Id);
    }
}
