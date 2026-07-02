using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.FilesDb.Configurations;

/// <summary>EF Core configuration for <see cref="DownloadedFileClassification" />.</summary>
public sealed class DownloadedFileClassificationConfiguration : IEntityTypeConfiguration<DownloadedFileClassification>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DownloadedFileClassification> builder)
    {
        _ = builder
           .ToTable(nameof(DownloadedFileClassification), Constants.SchemaName)
           .HasKey(d => d.Id);

        _ = builder.Property(d => d.FileDetailId)
                   .HasConversion(id => id.Value, id => new FileId(id))
                   .IsRequired();

        _ = builder.HasOne(d => d.FileDetail)
                   .WithMany()
                   .HasForeignKey(d => d.FileDetailId)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Cascade);

        _ = builder.HasOne(d => d.FileClassification)
                   .WithMany()
                   .HasForeignKey(d => d.FileClassificationId)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Cascade);

        _ = builder.HasIndex(d => new { d.FileDetailId, d.FileClassificationId }).IsUnique();
    }
}
