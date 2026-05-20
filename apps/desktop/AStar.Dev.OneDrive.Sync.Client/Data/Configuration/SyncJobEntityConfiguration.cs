using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Configuration;

public class SyncJobEntityConfiguration : IEntityTypeConfiguration<SyncJobEntity>
{
    public void Configure(EntityTypeBuilder<SyncJobEntity> builder)
    {
        _ = builder.HasKey(e => e.Id);
        _ = builder.Property(e => e.AccountId)
                   .HasConversion(id => id.Id, str => new AccountId(str));
        _ = builder.Property(e => e.FolderId)
                   .HasConversion(id => id.Id, str => new OneDriveFolderId(str));
        _ = builder.Property(e => e.RemoteItemId)
                   .HasConversion(id => id.Id, str => new OneDriveItemId(str));
        _ = builder.Property(e => e.ErrorMessage)
                   .HasConversion(SqliteTypeConverters.OptionStringToNullableString)
                   .IsRequired(false);
        _ = builder.Property(e => e.DownloadUrl)
                   .HasConversion(SqliteTypeConverters.OptionStringToNullableString)
                   .IsRequired(false);
        _ = builder.Property(e => e.CompletedAt)
                   .HasConversion(SqliteTypeConverters.OptionDateTimeOffsetToNullableTicks)
                   .IsRequired(false);
        _ = builder.HasIndex(j => new { j.AccountId, j.State });
    }
}
