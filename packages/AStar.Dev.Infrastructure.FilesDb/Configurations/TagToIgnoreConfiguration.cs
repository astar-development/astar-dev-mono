using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.FilesDb.Configurations;

/// <summary>
/// </summary>
public sealed class TagToIgnoreConfiguration : IEntityTypeConfiguration<TagToIgnore>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TagToIgnore> builder)
    {
        _ = builder
           .ToTable(nameof(TagToIgnore), Constants.SchemaName)
           .HasKey(tag => tag.Id);

        _ = builder.Property(tag => tag.Id)
                   .HasConversion(tagId => tagId.Value, tagId => new TagId(tagId));

        builder.Property(tag => tag.Value).HasMaxLength(300);
    }
}
