using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Configuration;

/// <summary>EF Core configuration for <see cref="FileClassificationKeywordEntity"/>.</summary>
public sealed class FileClassificationKeywordEntityConfiguration : IEntityTypeConfiguration<FileClassificationKeywordEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FileClassificationKeywordEntity> builder)
    {
        _ = builder.HasKey(e => e.Id);
        _ = builder.Property(e => e.Keyword).IsRequired();
        _ = builder.Property(e => e.IsSpecial).IsRequired();
        _ = builder.HasIndex(e => new { e.CategoryId, e.Keyword }).IsUnique();
        _ = builder.HasIndex(e => e.Keyword).IsUnique(false);
        //_ = builder.HasOne(e => e.Category).WithMany(e => e.Keywords).HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.Cascade);
    }
}
