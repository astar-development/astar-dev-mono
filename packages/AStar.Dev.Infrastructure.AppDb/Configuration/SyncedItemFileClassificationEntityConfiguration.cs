using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.AppDb.Configuration;

public sealed class SyncedItemFileClassificationEntityConfiguration : IEntityTypeConfiguration<SyncedItemFileClassificationEntity>
{
    public void Configure(EntityTypeBuilder<SyncedItemFileClassificationEntity> builder)
    {
        _ = builder.HasKey(e => e.Id);
        _ = builder.Property(e => e.FileDetailId).HasConversion(fileId => fileId!.Value.Id, guid => new FileId(guid));
        _ = builder.ToTable(t => t.HasCheckConstraint("CK_SyncedItemFileClassifications_ExactlyOneParent", "(\"SyncedItemId\" IS NULL AND \"FileDetailId\" IS NOT NULL) OR (\"SyncedItemId\" IS NOT NULL AND \"FileDetailId\" IS NULL)"));
        _ = builder.HasIndex(e => new { e.SyncedItemId, e.CategoryId }).IsUnique().HasFilter("\"SyncedItemId\" IS NOT NULL");
        _ = builder.HasIndex(e => new { e.FileDetailId, e.CategoryId }).IsUnique().HasFilter("\"FileDetailId\" IS NOT NULL");
        _ = builder.HasOne(e => e.SyncedItem)
                   .WithMany()
                   .HasForeignKey(e => e.SyncedItemId)
                   .OnDelete(DeleteBehavior.Cascade);
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
