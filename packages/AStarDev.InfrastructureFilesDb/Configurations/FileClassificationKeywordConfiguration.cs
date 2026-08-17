using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.FilesDb.Configurations;

/// <summary>
/// </summary>
public sealed class FileClassificationKeywordConfiguration : IEntityTypeConfiguration<FileClassificationKeyword>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FileClassificationKeyword> builder)
    {
        _ = builder
           .ToTable(nameof(FileClassificationKeyword), Constants.SchemaName)
           .HasKey(keyword => keyword.Id);

        _ = builder.Property(keyword => keyword.Keyword).HasMaxLength(150);

        _ = builder.HasIndex(keyword => new { keyword.CategoryId, keyword.Keyword }).IsUnique();

        _ = builder.HasOne(keyword => keyword.Category)
                   .WithMany(classification => classification.Keywords)
                   .HasForeignKey(keyword => keyword.CategoryId)
                   .OnDelete(DeleteBehavior.Cascade);
    }
}
