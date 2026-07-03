using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Configuration;

public sealed class FileClassificationKeywordEntityConfiguration : IEntityTypeConfiguration<FileClassificationKeywordEntity>
{
    public void Configure(EntityTypeBuilder<FileClassificationKeywordEntity> builder)
    {
        _ = builder.HasKey(e => e.Id);
        _ = builder.Property(e => e.Keyword).IsRequired().HasMaxLength(150);
        _ = builder.HasIndex(e => new { e.CategoryId, e.Keyword }).IsUnique();
        _ = builder.HasOne(e => e.Category)
                   .WithMany()
                   .HasForeignKey(e => e.CategoryId)
                   .OnDelete(DeleteBehavior.Cascade);
    }
}
