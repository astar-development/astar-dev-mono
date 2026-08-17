using AStar.Dev.Infrastructure.AppDb.Domain;
using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;
using OneDriveItemId = AStar.Dev.Infrastructure.AppDb.Entities.OneDriveItemId;

namespace AStar.Dev.Infrastructure.AppDb.Configuration;

/// <summary>EF Core configuration for <see cref="SyncConflictEntity"/>.</summary>
public class SyncConflictEntityConfiguration : IEntityTypeConfiguration<SyncConflictEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SyncConflictEntity> builder)
    {
        _ = builder.HasKey(e => e.Id);
        _ = builder.Property(e => e.AccountId)
                   .HasConversion(id => id.Value, str => new AccountId(str));
        _ = builder.Property(e => e.FolderId)
                   .HasConversion(id => id.Value, str => new OneDriveFolderId(str));
        _ = builder.Property(e => e.RemoteItemId)
                   .HasConversion(id => id.Value, str => new OneDriveItemId(str));
        _ = builder.Property(e => e.Resolution)
                   .HasConversion(SqliteTypeConverters.OptionConflictPolicyToNullableInt)
                   .IsRequired(false);
        _ = builder.Property(e => e.ResolvedAt)
                   .HasConversion(SqliteTypeConverters.OptionDateTimeOffsetToNullableTicks)
                   .IsRequired(false);
        _ = builder.HasIndex(c => new { c.AccountId, c.State });
    }
}
