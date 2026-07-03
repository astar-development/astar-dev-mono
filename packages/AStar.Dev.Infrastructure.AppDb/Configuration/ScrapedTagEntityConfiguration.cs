using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.AppDb.Configuration;

/// <summary>EF Core configuration for <see cref="ScrapedTagEntity"/>.</summary>
public sealed class ScrapedTagEntityConfiguration : IEntityTypeConfiguration<ScrapedTagEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ScrapedTagEntity> builder)
    {
        _ = builder.ToTable("ScrapedTag");
        _ = builder.HasKey(tag => tag.Id);
        _ = builder.Property(tag => tag.Id).HasConversion(tagId => tagId.Id, guid => new ScrapedTagId(guid));
        _ = builder.Property(tag => tag.Value).HasMaxLength(300).IsRequired();
        _ = builder.HasIndex(tag => tag.Value).IsUnique();
    }
}
