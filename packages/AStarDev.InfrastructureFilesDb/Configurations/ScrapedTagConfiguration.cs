using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.FilesDb.Configurations;

/// <summary>
/// </summary>
public sealed class ScrapedTagConfiguration : IEntityTypeConfiguration<ScrapedTag>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ScrapedTag> builder)
    {
        _ = builder
           .ToTable(nameof(ScrapedTag), Constants.SchemaName)
           .HasKey(tag => tag.Id);

        _ = builder.Property(tag => tag.Id)
                   .HasConversion(id => id.Value, id => new ScrapedTagId(id));

        builder.Property(tag => tag.Value).HasMaxLength(300).IsRequired();

        builder.HasIndex(tag => tag.Value).IsUnique();
    }
}
