using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.AppDb.Configuration;

/// <summary>EF Core configuration for <see cref="TagToIgnoreEntity"/>.</summary>
public sealed class TagToIgnoreEntityConfiguration : IEntityTypeConfiguration<TagToIgnoreEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TagToIgnoreEntity> builder)
    {
        _ = builder.ToTable("TagToIgnore");
        _ = builder.HasKey(tag => tag.Id);
        _ = builder.Property(tag => tag.Id).HasConversion(tagId => tagId.Id, guid => new TagId(guid));
        _ = builder.Property(tag => tag.Value).HasMaxLength(300);
    }
}
