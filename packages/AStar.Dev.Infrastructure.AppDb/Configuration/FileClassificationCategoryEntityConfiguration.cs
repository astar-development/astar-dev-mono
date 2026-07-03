using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.AppDb.Configuration;

/// <summary>EF Core configuration for <see cref="FileClassificationCategoryEntity"/>.</summary>
public sealed class FileClassificationCategoryEntityConfiguration : IEntityTypeConfiguration<FileClassificationCategoryEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FileClassificationCategoryEntity> builder)
    {
        _ = builder.HasKey(e => e.Id);
        _ = builder.Property(e => e.Name).IsRequired();
        _ = builder.HasIndex(e => new { e.ParentId, e.Name }).IsUnique();
        _ = builder.HasIndex(e => e.Name ).IsUnique(false);
    }
}
