using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Configuration;

/// <summary>EF Core configuration for <see cref="FileClassificationCategoryEntity"/>.</summary>
public sealed class FileClassificationCategoryEntityConfiguration : IEntityTypeConfiguration<FileClassificationCategoryEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FileClassificationCategoryEntity> builder)
    {
        _ = builder.HasKey(e => e.Id);
        _ = builder.Property(e => e.Name).IsRequired();
        _ = builder.HasIndex(e => new { e.ParentId, e.Name }).IsUnique();
        _ = builder.HasOne(e => e.Parent).WithMany(e => e.Children).HasForeignKey(e => e.ParentId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
    }
}
