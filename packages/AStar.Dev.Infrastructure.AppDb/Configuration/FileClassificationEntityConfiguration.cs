using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.AppDb.Configuration;

public sealed class FileClassificationEntityConfiguration : IEntityTypeConfiguration<FileClassificationEntity>
{
    public void Configure(EntityTypeBuilder<FileClassificationEntity> builder)
    {
        _ = builder.ToTable("FileClassifications");
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
