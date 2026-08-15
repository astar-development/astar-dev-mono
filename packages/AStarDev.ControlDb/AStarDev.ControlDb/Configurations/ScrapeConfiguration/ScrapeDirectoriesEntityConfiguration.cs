using AStarDev.ControlDb.ScrapeConfiguration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.ControlDb.Configurations.ScrapeConfiguration;

/// <summary>
/// Provides the configuration for the <see cref="ScrapeDirectoriesEntity"/> entity.
/// </summary>
public sealed class ScrapeDirectoriesEntityConfiguration : IEntityTypeConfiguration<ScrapeDirectoriesEntity>
{
    ///<inheritdoc/>
    public void Configure(EntityTypeBuilder<ScrapeDirectoriesEntity> builder)
    {
        builder.ToTable("ScrapeDirectories");

        builder.HasKey(sc => sc.Id);

        builder.Property(sc => sc.Id).ValueGeneratedOnAdd();
        builder.Property(d => d.Id).HasConversion(id => id.Value, value => new ScrapeDirectoriesId(value));
        builder.HasOne<ScrapeConfigurationEntity>()
            .WithOne(scrapeConfiguration => scrapeConfiguration.ScrapeDirectories)
            .HasForeignKey<ScrapeDirectoriesEntity>(scrapeDirectories => scrapeDirectories.ScrapeConfigurationEntityId)
            .HasPrincipalKey<ScrapeConfigurationEntity>(scrapeConfiguration => scrapeConfiguration.Id);
    }
}
