using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;
using OneDriveItemId = AStar.Dev.Infrastructure.AppDb.Entities.OneDriveItemId;

namespace AStar.Dev.Infrastructure.AppDb.Configuration;

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
        _ = builder.Property(e => e.FileDetailId).HasConversion(fileId => fileId!.Value.Id, guid => new FileId(guid));
        _ = builder.HasOne(e => e.FileDetail)
                   .WithMany()
                   .HasForeignKey(e => e.FileDetailId)
                   .OnDelete(DeleteBehavior.SetNull);

        _ = builder.HasIndex(i => i.RemotePath);
        _ = builder.HasIndex(i => i.LocalPath);
        _ = builder.HasIndex(i => i.SizeInBytes);
        _ = builder.HasIndex(e => new { e.AccountId, e.IsFolder, e.SizeInBytes });
        _ = builder.HasIndex(e => new { e.AccountId, e.IsFolder, e.RemotePath });
    }
}
