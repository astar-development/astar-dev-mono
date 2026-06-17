using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Configuration;

public sealed class SyncedItemFileClassificationEntityConfiguration : IEntityTypeConfiguration<SyncedItemFileClassificationEntity>
{
    public void Configure(EntityTypeBuilder<SyncedItemFileClassificationEntity> builder)
    {
        _ = builder.HasKey(e => e.Id);
        _ = builder.HasIndex(e => new { e.SyncedItemId, e.CategoryId }).IsUnique();
        _ = builder.HasOne(e => e.SyncedItem)
                   .WithMany()
                   .HasForeignKey(e => e.SyncedItemId)
                   .OnDelete(DeleteBehavior.Cascade);
        _ = builder.HasOne(e => e.Category)
                   .WithMany()
                   .HasForeignKey(e => e.CategoryId)
                   .OnDelete(DeleteBehavior.Restrict);
    }
}
