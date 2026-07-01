using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.FilesDb.Configurations;

internal sealed class ConnectionStringsConfiguration : IEntityTypeConfiguration<ConnectionStrings>
{
    public void Configure(EntityTypeBuilder<ConnectionStrings> builder)
    {
        builder.ToTable("ConnectionStrings");

        builder.HasKey(connectionStrings => connectionStrings.Id);

        builder.HasIndex(connectionStrings => connectionStrings.ScrapeConfigurationEntityId)
               .IsUnique();

        builder.Property(connectionStrings => connectionStrings.ScrapeConfigurationEntityId)
               .IsRequired();

        builder.Property(connectionStrings => connectionStrings.Sqlite)
               .HasColumnType("nvarchar(256)")
               .IsRequired();
    }
}