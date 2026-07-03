using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Configuration;

public sealed class DownloadedFileClassificationEntityConfiguration : IEntityTypeConfiguration<DownloadedFileClassificationEntity>
{
    public void Configure(EntityTypeBuilder<DownloadedFileClassificationEntity> builder)
    {
        _ = builder.HasKey(e => e.Id);
        _ = builder.Property(e => e.FileDetailId).HasConversion(fileId => fileId.Id, guid => new FileId(guid));
        _ = builder.HasIndex(e => new { e.FileDetailId, e.CategoryId }).IsUnique();
        _ = builder.HasOne(e => e.FileDetail)
                   .WithMany()
                   .HasForeignKey(e => e.FileDetailId)
                   .OnDelete(DeleteBehavior.Cascade);
        _ = builder.HasOne(e => e.Category)
                   .WithMany()
                   .HasForeignKey(e => e.CategoryId)
                   .OnDelete(DeleteBehavior.Restrict);
    }
}
