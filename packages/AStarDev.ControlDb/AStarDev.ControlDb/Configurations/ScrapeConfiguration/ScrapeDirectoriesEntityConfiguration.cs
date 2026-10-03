using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.EFCoreSqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.ControlDb.Configurations.ScrapeConfiguration;

/// <summary>Provides the configuration for the <see cref="ScrapeDirectoriesEntity"/> entity.</summary>
public sealed class ScrapeDirectoriesEntityConfiguration : IEntityTypeConfiguration<ScrapeDirectoriesEntity>
{
    ///<inheritdoc/>
    public void Configure(EntityTypeBuilder<ScrapeDirectoriesEntity> builder)
    {
        _ = builder.ToTable("ScrapeDirectories");

        _ = builder.HasKey(sc => sc.Id);

        _ = builder.Property(sc => sc.Id).ValueGeneratedOnAdd();
        _ = builder.Property(d => d.Id).HasConversion(id => id.Value, value => new ScrapeDirectoriesId(value));
        _ = builder.Property(sc => sc.Id).HasValueGenerator((_, _) => new StrongGuidIdValueGenerator<ScrapeDirectoriesId>(value => new ScrapeDirectoriesId(value)));
        _ = builder.HasOne<ScrapeConfigurationEntity>()
            .WithOne(scrapeConfiguration => scrapeConfiguration.ScrapeDirectories)
            .HasForeignKey<ScrapeDirectoriesEntity>(scrapeDirectories => scrapeDirectories.ScrapeConfigurationEntityId)
            .HasPrincipalKey<ScrapeConfigurationEntity>(scrapeConfiguration => scrapeConfiguration.Id);
    }
}
