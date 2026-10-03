using AStarDev.ControlDb.TagDetail;
using AStarDev.EFCoreSqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.ControlDb.Configurations.Tags;

/// <summary>EF Core configuration for <see cref="TagEntity"/>.</summary>
public sealed class TagEntityConfiguration : IEntityTypeConfiguration<TagEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TagEntity> builder)
    {
        _ = builder.ToTable("Tags");

        _ = builder.HasKey(tag => tag.Id);
        _ = builder.Property(tag => tag.Id).ValueGeneratedOnAdd();
        _ = builder.Property(tag => tag.Id).HasConversion(id => id.Value, guid => new TagId(guid));
        _ = builder.Property(tag => tag.Id).HasValueGenerator((_, _) => new StrongGuidIdValueGenerator<TagId>(value => new TagId(value)));

        _ = builder.HasIndex(tag => tag.WallhavenTagId).IsUnique();
    }
}
