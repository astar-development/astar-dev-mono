using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Configuration;

public sealed class SyncedItemClassificationEntityConfiguration : IEntityTypeConfiguration<SyncedItemClassificationEntity>
{
    public void Configure(EntityTypeBuilder<SyncedItemClassificationEntity> builder)
    {
        _ = builder.HasKey(e => e.Id);
        _ = builder.Property(e => e.Level1).IsRequired();
        _ = builder.Property(e => e.Level2).IsRequired(false);
        _ = builder.Property(e => e.Level3).IsRequired(false);
        _ = builder.Property(e => e.TagName).IsRequired();
        _ = builder.HasIndex(e => e.TagName);
        _ = builder.HasOne(e => e.SyncedItem)
                   .WithMany()
                   .HasForeignKey(e => e.SyncedItemId)
                   .OnDelete(DeleteBehavior.Cascade);
    }
}
