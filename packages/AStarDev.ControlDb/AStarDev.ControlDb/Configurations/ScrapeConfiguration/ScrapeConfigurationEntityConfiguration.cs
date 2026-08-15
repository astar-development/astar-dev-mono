using AStarDev.ControlDb.ScrapeConfiguration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.ControlDb.Configurations.ScrapeConfiguration;

/// <summary>
/// Provides the configuration for the <see cref="ScrapeConfigurationEntity"/> entity.
/// </summary>
public sealed class ScrapeConfigurationEntityConfiguration : IEntityTypeConfiguration<ScrapeConfigurationEntity>
{
    ///<inheritdoc/>
    public void Configure(EntityTypeBuilder<ScrapeConfigurationEntity> builder)
    {
        builder.ToTable("ScrapeConfigurations");

        builder.HasKey(sc => sc.Id);

        builder.Property(sc => sc.Id).ValueGeneratedOnAdd();
        builder.Property(d => d.Id).HasConversion(id => id.Value, value => new ScrapeConfigurationId(value));
    }
}
