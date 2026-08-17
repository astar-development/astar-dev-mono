using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.AppDb.Configuration;

/// <summary>EF Core configuration for <see cref="ScrapeConfigurationEntity"/>.</summary>
public sealed class ScrapeConfigurationEntityConfiguration : IEntityTypeConfiguration<ScrapeConfigurationEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ScrapeConfigurationEntity> builder)
    {
        _ = builder.ToTable("ScrapeConfiguration");
        _ = builder.HasKey(config => config.Id);

        _ = builder.HasOne(config => config.ConnectionStrings)
                   .WithOne(connectionStrings => connectionStrings.ScrapeConfigurationEntity)
                   .HasForeignKey<ConnectionStringsEntity>(connectionStrings => connectionStrings.ScrapeConfigurationEntityId)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Cascade);

        _ = builder.HasOne(config => config.UserConfiguration)
                   .WithOne(userConfiguration => userConfiguration.ScrapeConfigurationEntity)
                   .HasForeignKey<UserConfigurationEntity>(userConfiguration => userConfiguration.ScrapeConfigurationEntityId)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Cascade);

        _ = builder.HasOne(config => config.SearchConfiguration)
                   .WithOne(searchConfiguration => searchConfiguration.ScrapeConfigurationEntity)
                   .HasForeignKey<SearchConfigurationEntity>(searchConfiguration => searchConfiguration.ScrapeConfigurationEntityId)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Cascade);

        _ = builder.HasOne(config => config.ScrapeDirectories)
                   .WithOne(scrapeDirectories => scrapeDirectories.ScrapeConfigurationEntity)
                   .HasForeignKey<ScrapeDirectoriesEntity>(scrapeDirectories => scrapeDirectories.ScrapeConfigurationEntityId)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Cascade);
    }
}
