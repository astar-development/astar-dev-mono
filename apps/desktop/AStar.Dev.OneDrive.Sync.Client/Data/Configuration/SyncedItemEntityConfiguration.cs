using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Configuration;

public class SyncedItemEntityConfiguration : IEntityTypeConfiguration<SyncedItemEntity>
{
    public void Configure(EntityTypeBuilder<SyncedItemEntity> builder)
    {
        _ = builder.HasKey(e => e.Id);
        _ = builder.Property(e => e.AccountId)
                   .HasConversion(id => id.Id, str => new AccountId(str));
        _ = builder.Property(e => e.RemoteItemId)
                   .HasConversion(id => id.Id, str => new OneDriveItemId(str));
        _ = builder.HasIndex(e => new { e.AccountId, e.RemoteItemId }).IsUnique();
        _ = builder.HasIndex(e => new { e.AccountId, e.LocalPath });
        _ = builder.HasIndex(e => new { e.AccountId, e.SizeInBytes });
        _ = builder.OwnsOne(e => e.Tags, b =>
        {
            _ = b.Property(v => v.ETag).HasColumnName("ETag")
                 .HasConversion(SqliteTypeConverters.OptionStringToNullableString)
                 .IsRequired(false);
            _ = b.Property(v => v.CTag).HasColumnName("CTag")
                 .HasConversion(SqliteTypeConverters.OptionStringToNullableString)
                 .IsRequired(false);
        });
        _ = builder.HasOne(e => e.Account)
                   .WithMany()
                   .HasForeignKey(e => e.AccountId)
                   .OnDelete(DeleteBehavior.Cascade);
    }
}
