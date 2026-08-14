using AStarDev.ControlDb.ScrapeConfiguration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.ControlDb.Configurations.ScrapeConfiguration;

/// <summary>
/// Provides the configuration for the <see cref="SearchConfigurationEntity"/> entity.
/// </summary>
public sealed class SearchConfigurationEntityConfiguration : IEntityTypeConfiguration<SearchConfigurationEntity>
{
    ///<inheritdoc/>
    public void Configure(EntityTypeBuilder<SearchConfigurationEntity> builder)
    {
        builder.ToTable("SearchConfigurations");

        builder.HasKey(sc => sc.Id);

        builder.Property(sc => sc.Id).ValueGeneratedOnAdd();
        builder.Property(d => d.Id).HasConversion(id => id.Value, value => new SearchConfigurationId(value));
        builder.HasOne<ScrapeConfigurationEntity>()
            .WithOne(scrapeConfiguration => scrapeConfiguration.SearchConfiguration)
            .HasForeignKey<SearchConfigurationEntity>(searchConfiguration => searchConfiguration.ScrapeConfigurationEntityId)
            .HasPrincipalKey<ScrapeConfigurationEntity>(scrapeConfiguration => scrapeConfiguration.Id);
    }
}
