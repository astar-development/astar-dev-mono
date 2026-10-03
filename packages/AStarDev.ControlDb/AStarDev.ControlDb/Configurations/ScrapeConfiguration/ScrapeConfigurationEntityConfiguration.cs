using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.EFCoreSqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.ControlDb.Configurations.ScrapeConfiguration;

/// <summary>Provides the configuration for the <see cref="ScrapeConfigurationEntity"/> entity.</summary>
public sealed class ScrapeConfigurationEntityConfiguration : IEntityTypeConfiguration<ScrapeConfigurationEntity>
{
    ///<inheritdoc/>
    public void Configure(EntityTypeBuilder<ScrapeConfigurationEntity> builder)
    {
        _ = builder.ToTable("ScrapeConfigurations");

        _ = builder.HasKey(sc => sc.Id);

        _ = builder.Property(sc => sc.Id).ValueGeneratedOnAdd();
        _ = builder.Property(d => d.Id).HasConversion(id => id.Value, value => new ScrapeConfigurationId(value));
        _ = builder.Property(sc => sc.Id).HasValueGenerator((_, _) => new StrongGuidIdValueGenerator<ScrapeConfigurationId>(value => new ScrapeConfigurationId(value)));
    }
}
