using AStar.Dev.Infrastructure.AppDb.Domain;
using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;
using OneDriveItemId = AStar.Dev.Infrastructure.AppDb.Entities.OneDriveItemId;

namespace AStar.Dev.Infrastructure.AppDb.Configuration;

/// <summary>EF Core configuration for <see cref="SyncJobEntity"/>.</summary>
public class SyncJobEntityConfiguration : IEntityTypeConfiguration<SyncJobEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SyncJobEntity> builder)
    {
        _ = builder.HasKey(e => e.Id);
        _ = builder.Property(e => e.AccountId)
                   .HasConversion(id => id.Value, str => new AccountId(str));
        _ = builder.Property(e => e.FolderId)
                   .HasConversion(id => id.Value, str => new OneDriveFolderId(str));
        _ = builder.Property(e => e.RemoteItemId)
                   .HasConversion(id => id.Value, str => new OneDriveItemId(str));
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
