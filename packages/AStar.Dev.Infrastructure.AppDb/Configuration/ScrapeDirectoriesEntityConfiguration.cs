using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.AppDb.Configuration;

/// <summary>EF Core configuration for <see cref="ScrapeDirectoriesEntity"/>.</summary>
public sealed class ScrapeDirectoriesEntityConfiguration : IEntityTypeConfiguration<ScrapeDirectoriesEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ScrapeDirectoriesEntity> builder)
    {
        _ = builder.ToTable("ScrapeDirectories");
        _ = builder.HasKey(scrapeDirectories => scrapeDirectories.Id);
        _ = builder.HasIndex(scrapeDirectories => scrapeDirectories.ScrapeConfigurationEntityId).IsUnique();
        _ = builder.Property(scrapeDirectories => scrapeDirectories.ScrapeConfigurationEntityId).IsRequired();
        _ = builder.Property(scrapeDirectories => scrapeDirectories.RootDirectory).HasMaxLength(256);
        _ = builder.Property(scrapeDirectories => scrapeDirectories.BaseSaveDirectory).HasMaxLength(256);
        _ = builder.Property(scrapeDirectories => scrapeDirectories.BaseDirectory).HasMaxLength(256);
        _ = builder.Property(scrapeDirectories => scrapeDirectories.BaseDirectoryFamous).HasMaxLength(256);
        _ = builder.Property(scrapeDirectories => scrapeDirectories.SubDirectoryName).HasMaxLength(256);
    }
}
