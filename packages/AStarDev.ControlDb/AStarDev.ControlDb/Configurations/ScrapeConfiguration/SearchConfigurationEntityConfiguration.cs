using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.EFCoreSqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.ControlDb.Configurations.ScrapeConfiguration;

/// <summary>Provides the configuration for the <see cref="SearchConfigurationEntity"/> entity.</summary>
public sealed class SearchConfigurationEntityConfiguration : IEntityTypeConfiguration<SearchConfigurationEntity>
{
    ///<inheritdoc/>
    public void Configure(EntityTypeBuilder<SearchConfigurationEntity> builder)
    {
        _ = builder.ToTable("SearchConfigurations");

        _ = builder.HasKey(sc => sc.Id);

        _ = builder.Property(sc => sc.Id).ValueGeneratedOnAdd();
        _ = builder.Property(d => d.Id).HasConversion(id => id.Value, value => new SearchConfigurationId(value));
        _ = builder.Property(sc => sc.Id).HasValueGenerator((_, _) => new StrongGuidIdValueGenerator<SearchConfigurationId>(value => new SearchConfigurationId(value)));
        _ = builder.HasOne<ScrapeConfigurationEntity>()
            .WithOne(scrapeConfiguration => scrapeConfiguration.SearchConfiguration)
            .HasForeignKey<SearchConfigurationEntity>(searchConfiguration => searchConfiguration.ScrapeConfigurationId)
            .HasPrincipalKey<ScrapeConfigurationEntity>(scrapeConfiguration => scrapeConfiguration.Id);

        _ = builder.HasMany(searchConfiguration => searchConfiguration.SearchCategories)
            .WithOne(searchCategory => searchCategory.SearchConfiguration)
            .HasForeignKey(searchCategory => searchCategory.SearchConfigurationId)
            .HasPrincipalKey(searchConfiguration => searchConfiguration.Id);

        _ = builder.HasMany(searchConfiguration => searchConfiguration.PersonCategories)
            .WithOne(personCategory => personCategory.SearchConfiguration)
            .HasForeignKey(personCategory => personCategory.SearchConfigurationId)
            .HasPrincipalKey(searchConfiguration => searchConfiguration.Id);
    }
}
