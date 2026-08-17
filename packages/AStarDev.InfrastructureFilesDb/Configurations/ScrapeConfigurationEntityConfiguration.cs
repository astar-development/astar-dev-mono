using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.FilesDb.Configurations;

/// <summary>
/// </summary>
public sealed class ScrapeConfigurationEntityConfiguration : IEntityTypeConfiguration<ScrapeConfigurationEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ScrapeConfigurationEntity> builder)
    {
        _ = builder.ToTable("ScrapeConfiguration");

        _ = builder.HasKey(config => config.Id);

        _ = builder.HasOne(config => config.ConnectionStrings)
                   .WithOne(connectionStrings => connectionStrings.ScrapeConfigurationEntity)
                   .HasForeignKey<ConnectionStrings>(connectionStrings => connectionStrings.ScrapeConfigurationEntityId)
                   .IsRequired();

        _ = builder.HasOne(config => config.UserConfiguration)
                   .WithOne(userConfiguration => userConfiguration.ScrapeConfigurationEntity)
                   .HasForeignKey<UserConfiguration>(userConfiguration => userConfiguration.ScrapeConfigurationEntityId)
                   .IsRequired();

        _ = builder.HasOne(config => config.SearchConfiguration)
                   .WithOne(searchConfiguration => searchConfiguration.ScrapeConfigurationEntity)
                   .HasForeignKey<SearchConfiguration>(searchConfiguration => searchConfiguration.ScrapeConfigurationEntityId)
                   .IsRequired();

        _ = builder.HasOne(config => config.ScrapeDirectories)
                   .WithOne(scrapeDirectories => scrapeDirectories.ScrapeConfigurationEntity)
                   .HasForeignKey<ScrapeDirectories>(scrapeDirectories => scrapeDirectories.ScrapeConfigurationEntityId)
                   .IsRequired();
    }
}
