using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.FilesDb.Configurations;

internal sealed class ScrapeDirectoriesConfiguration : IEntityTypeConfiguration<ScrapeDirectories>
{
    public void Configure(EntityTypeBuilder<ScrapeDirectories> builder)
    {
        builder.ToTable("ScrapeDirectories");

        builder.HasKey(scrapeDirectories => scrapeDirectories.Id);

        builder.HasIndex(scrapeDirectories => scrapeDirectories.ScrapeConfigurationEntityId)
               .IsUnique();

        builder.Property(scrapeDirectories => scrapeDirectories.ScrapeConfigurationEntityId)
               .IsRequired();

        builder.Property(scrapeDirectories => scrapeDirectories.RootDirectory)
               .HasColumnType("nvarchar(256)");

        builder.Property(scrapeDirectories => scrapeDirectories.BaseSaveDirectory)
               .HasColumnType("nvarchar(256)");

        builder.Property(scrapeDirectories => scrapeDirectories.BaseDirectory)
               .HasColumnType("nvarchar(256)");

        builder.Property(scrapeDirectories => scrapeDirectories.BaseDirectoryFamous)
               .HasColumnType("nvarchar(256)");

        builder.Property(scrapeDirectories => scrapeDirectories.SubDirectoryName)
               .HasColumnType("nvarchar(256)");
    }
}
